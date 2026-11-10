using MedicalAppointments.Application.Appointments;
using MedicalAppointments.Application.ClinicalRecords.Auth;
using MedicalAppointments.Application.ClinicalRecords.Common;
using MedicalAppointments.Application.ClinicalRecords.Outbox;
using MedicalAppointments.Application.ClinicalRecords.Stores;
using MedicalAppointments.Application.Common;
using MedicalAppointments.Application.Common.Audit;
using MedicalAppointments.Application.Common.Exceptions;
using MedicalAppointments.Application.MedicalStructure.Doctors;
using MedicalAppointments.Domain.ClinicalRecords;

namespace MedicalAppointments.Application.ClinicalRecords;

public sealed class DiagnosisService : IDiagnosisService
{
    private readonly IClinicalRecordsAccessService _access;
    private readonly IDiagnosisStore _store;
    private readonly IAppointmentStore _appointmentStore;
    private readonly IDoctorStore _doctorStore;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClinicalRecordsEventNotifier _notifier;
    private readonly IAuditWriter _auditWriter;
    private readonly IRequestMetadataProvider _requestMetadata;
    private readonly ICorrelationIdProvider _correlationIdProvider;
    private readonly TimeProvider _timeProvider;

    public DiagnosisService(
        IClinicalRecordsAccessService access,
        IDiagnosisStore store,
        IAppointmentStore appointmentStore,
        IDoctorStore doctorStore,
        IUnitOfWork unitOfWork,
        IClinicalRecordsEventNotifier notifier,
        IAuditWriter auditWriter,
        IRequestMetadataProvider requestMetadata,
        ICorrelationIdProvider correlationIdProvider,
        TimeProvider timeProvider)
    {
        _access = access;
        _store = store;
        _appointmentStore = appointmentStore;
        _doctorStore = doctorStore;
        _unitOfWork = unitOfWork;
        _notifier = notifier;
        _auditWriter = auditWriter;
        _requestMetadata = requestMetadata;
        _correlationIdProvider = correlationIdProvider;
        _timeProvider = timeProvider;
    }

    public async Task<IReadOnlyList<DiagnosisDto>> ListByAppointmentAsync(Guid appointmentId, CancellationToken cancellationToken = default)
    {
        var subject = await _access.GetSubjectAsync(cancellationToken);
        var appointment = await _appointmentStore.GetTrackedByIdAsync(appointmentId, cancellationToken)
            ?? throw new NotFoundException("Appointment not found.");

        if (!_access.CanReadNotesForAppointment(appointment, subject))
        {
            throw new ForbiddenException("You do not have access to this appointment's clinical records.");
        }

        var items = await _store.GetByAppointmentAsync(appointmentId, cancellationToken);
        return items.Select(Map).ToList();
    }

    public async Task<DiagnosisDto> CreateAsync(CreateDiagnosisCommand command, CancellationToken cancellationToken = default)
    {
        var now = _timeProvider.GetUtcNow();
        var subject = await _access.GetSubjectAsync(cancellationToken);
        var appointment = await LoadAndGateStatusAsync(command.AppointmentId, cancellationToken);

        if (!subject.CanCreate)
        {
            throw new ForbiddenException("You do not have permission to record diagnoses.");
        }

        var (doctorId, displayName) = await ResolveAuthorProfileAsync(subject, cancellationToken);
        if (!(subject.CanManageAll || subject.DoctorId == appointment.DoctorId))
        {
            throw new ForbiddenException("You may only record diagnoses for your own appointments.");
        }

        var recordedAt = command.RecordedAtUtc ?? now;
        var diagnosis = Diagnosis.Create(
            appointment.Id, subject.UserId!.Value, doctorId, displayName,
            command.Code, command.DisplayTerm, command.Category, command.OnsetDate, command.Note,
            recordedAt, now);

        await _store.AddAsync(diagnosis, cancellationToken);
        _notifier.EnqueueDiagnosisEvent(diagnosis, appointment, "Created", now, _correlationIdProvider.CorrelationId);
        EnqueueAudit(subject.UserId, "ClinicalRecords.DiagnosisCreated", diagnosis.Id, now, new { appointmentId = appointment.Id, code = diagnosis.Code });
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Map(diagnosis);
    }

    public async Task<DiagnosisDto> UpdateAsync(UpdateDiagnosisCommand command, CancellationToken cancellationToken = default)
    {
        var now = _timeProvider.GetUtcNow();
        var subject = await _access.GetSubjectAsync(cancellationToken);
        var (diagnosis, appointment) = await LoadDiagnosisAndAppointmentAsync(command.Id, cancellationToken);

        if (!subject.CanUpdate || !(subject.CanManageAll || diagnosis.AuthorUserId == subject.UserId))
        {
            throw new ForbiddenException("You do not have permission to edit this diagnosis.");
        }

        diagnosis.UpdateDraft(command.Code, command.DisplayTerm, command.Category, command.OnsetDate, command.Note, now);
        _notifier.EnqueueDiagnosisEvent(diagnosis, appointment, "Updated", now, _correlationIdProvider.CorrelationId);
        EnqueueAudit(subject.UserId, "ClinicalRecords.DiagnosisUpdated", diagnosis.Id, now, null);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Map(diagnosis);
    }

    public async Task<DiagnosisDto> FinalizeAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var now = _timeProvider.GetUtcNow();
        var subject = await _access.GetSubjectAsync(cancellationToken);
        var (diagnosis, appointment) = await LoadDiagnosisAndAppointmentAsync(id, cancellationToken);

        if (!subject.CanFinalize || !(subject.CanManageAll || diagnosis.AuthorUserId == subject.UserId))
        {
            throw new ForbiddenException("You do not have permission to finalize this diagnosis.");
        }

        diagnosis.Finalize(subject.UserId!.Value, now);
        _notifier.EnqueueDiagnosisEvent(diagnosis, appointment, "Finalized", now, _correlationIdProvider.CorrelationId);
        EnqueueAudit(subject.UserId, "ClinicalRecords.DiagnosisFinalized", diagnosis.Id, now, null);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Map(diagnosis);
    }

    public async Task<DiagnosisDto> CorrectAsync(CorrectDiagnosisCommand command, CancellationToken cancellationToken = default)
    {
        var now = _timeProvider.GetUtcNow();
        var subject = await _access.GetSubjectAsync(cancellationToken);
        var original = await _store.GetTrackedByIdAsync(command.OriginalId, cancellationToken)
            ?? throw new NotFoundException("Diagnosis not found.");
        var appointment = await _appointmentStore.GetTrackedByIdAsync(original.AppointmentId, cancellationToken)
            ?? throw new NotFoundException("Appointment not found.");

        if (!subject.CanCorrect || !(subject.CanManageAll || original.AuthorUserId == subject.UserId))
        {
            throw new ForbiddenException("You do not have permission to correct this diagnosis.");
        }

        var (doctorId, displayName) = await ResolveAuthorProfileAsync(subject, cancellationToken);
        var correction = Diagnosis.CreateCorrection(
            original, appointment.Id, subject.UserId!.Value, doctorId, displayName,
            command.Code, command.DisplayTerm, command.Category, command.OnsetDate, command.Note,
            command.RecordedAtUtc ?? now, now);

        await _store.AddAsync(correction, cancellationToken);
        _notifier.EnqueueDiagnosisEvent(correction, appointment, "Created", now, _correlationIdProvider.CorrelationId);
        EnqueueAudit(subject.UserId, "ClinicalRecords.DiagnosisCorrected", correction.Id, now, new { correctsId = original.Id });
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Map(correction);
    }

    private async Task<Domain.Appointments.Appointment> LoadAndGateStatusAsync(Guid appointmentId, CancellationToken cancellationToken)
    {
        var appointment = await _appointmentStore.GetTrackedByIdAsync(appointmentId, cancellationToken)
            ?? throw new NotFoundException("Appointment not found.");

        if (appointment.Status != Domain.Appointments.AppointmentStatus.InProgress
            && appointment.Status != Domain.Appointments.AppointmentStatus.Completed)
        {
            throw new ConflictException("Diagnoses can only be recorded while the appointment is InProgress or Completed.");
        }

        return appointment;
    }

    private async Task<(Diagnosis Diagnosis, Domain.Appointments.Appointment Appointment)> LoadDiagnosisAndAppointmentAsync(Guid id, CancellationToken cancellationToken)
    {
        var diagnosis = await _store.GetTrackedByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException("Diagnosis not found.");
        var appointment = await _appointmentStore.GetTrackedByIdAsync(diagnosis.AppointmentId, cancellationToken)
            ?? throw new NotFoundException("Appointment not found.");
        return (diagnosis, appointment);
    }

    private async Task<(Guid DoctorId, string DisplayName)> ResolveAuthorProfileAsync(
        ClinicalRecordsAccessSubject subject,
        CancellationToken cancellationToken)
    {
        // FIX 8d.1 reusado literal: IDoctorStore NO tiene GetTrackedByIdAsync; la única firma de tu body
        // que devuelve el Doctor CON nombre es GetTrackedWithRelationsAsync. No se asume simetría entre stores.
        var doctorId = await _doctorStore.GetIdByUserIdAsync(subject.UserId!.Value, cancellationToken);
        if (doctorId is null)
        {
            throw new ConflictException("Recording diagnoses requires a doctor profile linked to your account.");
        }

        var doctor = await _doctorStore.GetTrackedWithRelationsAsync(doctorId.Value, cancellationToken);
        var displayName = doctor is null ? string.Empty : $"{doctor.FirstName} {doctor.LastName}".Trim();
        if (string.IsNullOrWhiteSpace(displayName))
        {
            throw new ConflictException("Author display name could not be resolved from the doctor profile.");
        }

        return (doctorId.Value, displayName);
    }

    private void EnqueueAudit(Guid? userId, string action, Guid entityId, DateTimeOffset now, object? newValues)
    {
        _auditWriter.Enqueue(new AuditEntry(
            userId, action, nameof(Diagnosis), entityId, now,
            _requestMetadata.IpAddress, _requestMetadata.UserAgent, null,
            newValues is null ? null : System.Text.Json.JsonSerializer.Serialize(newValues, JsonDefaults.Options),
            _correlationIdProvider.CorrelationId));
    }

    private static DiagnosisDto Map(Diagnosis d) => new(
        d.Id, d.AppointmentId, d.AuthorUserId, d.AuthorDoctorId, d.AuthorDisplayName,
        d.Code, d.DisplayTerm, d.Category, d.OnsetDate, d.Note,
        d.IsFinal, d.RecordedAtUtc, d.FinalizedAt, d.FinalizedByUserId, d.CorrectsDiagnosisId,
        null, d.CreatedAt);
}