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
using Microsoft.Extensions.Options;

namespace MedicalAppointments.Application.ClinicalRecords;

public sealed class VitalSignsService : IVitalSignsService
{
    private readonly IClinicalRecordsAccessService _access;
    private readonly IVitalSignsStore _store;
    private readonly IAppointmentStore _appointmentStore;
    private readonly IDoctorStore _doctorStore;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClinicalRecordsEventNotifier _notifier;
    private readonly IAuditWriter _auditWriter;
    private readonly IRequestMetadataProvider _requestMetadata;
    private readonly ICorrelationIdProvider _correlationIdProvider;
    private readonly TimeProvider _timeProvider;

    public VitalSignsService(
        IClinicalRecordsAccessService access,
        IVitalSignsStore store,
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

    public async Task<IReadOnlyList<VitalSignsDto>> ListByAppointmentAsync(Guid appointmentId, CancellationToken cancellationToken = default)
    {
        var subject = await _access.GetSubjectAsync(cancellationToken);
        var appointment = await _appointmentStore.GetTrackedByIdAsync(appointmentId, cancellationToken)
            ?? throw new NotFoundException("Appointment not found.");

        if (!_access.CanReadNotesForAppointment(appointment, subject))   // método confirmado 8b/8c
        {
            throw new ForbiddenException("You do not have access to this appointment's clinical records.");
        }

        var items = await _store.GetByAppointmentAsync(appointmentId, cancellationToken);
        return items.Select(Map).ToList();
    }

    public async Task<VitalSignsDto> CreateAsync(CreateVitalSignsCommand command, CancellationToken cancellationToken = default)
    {
        var now = _timeProvider.GetUtcNow();
        var subject = await _access.GetSubjectAsync(cancellationToken);
        var appointment = await LoadAndGateStatusAsync(command.AppointmentId, cancellationToken);

        // Gate de creación inline con flags CONFIRMADOS (no asumo método CanCreateNote en el access service).
        if (!subject.CanCreate)
        {
            throw new ForbiddenException("You do not have permission to record vital signs.");
        }

        var (doctorId, displayName) = await ResolveAuthorProfileAsync(subject, cancellationToken);
        if (!(subject.CanManageAll || subject.DoctorId == appointment.DoctorId))
        {
            throw new ForbiddenException("You may only record vital signs for your own appointments.");
        }

        var recordedAt = command.RecordedAtUtc ?? now;
        var vitalSigns = VitalSigns.Create(
            appointment.Id, subject.UserId!.Value, doctorId, displayName,
            command.Measurements, recordedAt, now);

        await _store.AddAsync(vitalSigns, cancellationToken);
        _notifier.EnqueueVitalSignsEvent(vitalSigns, appointment, "Created", now, _correlationIdProvider.CorrelationId);
        EnqueueAudit(subject.UserId, "ClinicalRecords.VitalSignsCreated", vitalSigns.Id, now, new { appointmentId = appointment.Id });
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Map(vitalSigns);
    }

    public async Task<VitalSignsDto> UpdateAsync(UpdateVitalSignsCommand command, CancellationToken cancellationToken = default)
    {
        var now = _timeProvider.GetUtcNow();
        var subject = await _access.GetSubjectAsync(cancellationToken);
        var (vitalSigns, appointment) = await LoadVitalSignsAndAppointmentAsync(command.Id, cancellationToken);

        if (!subject.CanUpdate || !(subject.CanManageAll || vitalSigns.AuthorUserId == subject.UserId))
        {
            throw new ForbiddenException("You do not have permission to edit this vital-sign record.");
        }

        vitalSigns.UpdateDraft(command.Measurements, now);
        _notifier.EnqueueVitalSignsEvent(vitalSigns, appointment, "Updated", now, _correlationIdProvider.CorrelationId);
        EnqueueAudit(subject.UserId, "ClinicalRecords.VitalSignsUpdated", vitalSigns.Id, now, null);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Map(vitalSigns);
    }

    public async Task<VitalSignsDto> FinalizeAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var now = _timeProvider.GetUtcNow();
        var subject = await _access.GetSubjectAsync(cancellationToken);
        var (vitalSigns, appointment) = await LoadVitalSignsAndAppointmentAsync(id, cancellationToken);

        if (!subject.CanFinalize || !(subject.CanManageAll || vitalSigns.AuthorUserId == subject.UserId))
        {
            throw new ForbiddenException("You do not have permission to finalize this vital-sign record.");
        }

        vitalSigns.Finalize(subject.UserId!.Value, now);
        _notifier.EnqueueVitalSignsEvent(vitalSigns, appointment, "Finalized", now, _correlationIdProvider.CorrelationId);
        EnqueueAudit(subject.UserId, "ClinicalRecords.VitalSignsFinalized", vitalSigns.Id, now, null);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Map(vitalSigns);
    }

    public async Task<VitalSignsDto> CorrectAsync(CorrectVitalSignsCommand command, CancellationToken cancellationToken = default)
    {
        var now = _timeProvider.GetUtcNow();
        var subject = await _access.GetSubjectAsync(cancellationToken);
        var original = await _store.GetTrackedByIdAsync(command.OriginalId, cancellationToken)
            ?? throw new NotFoundException("Vital-sign record not found.");
        var appointment = await _appointmentStore.GetTrackedByIdAsync(original.AppointmentId, cancellationToken)
            ?? throw new NotFoundException("Appointment not found.");

        if (!subject.CanCorrect || !(subject.CanManageAll || original.AuthorUserId == subject.UserId))
        {
            throw new ForbiddenException("You do not have permission to correct this vital-sign record.");
        }

        var (doctorId, displayName) = await ResolveAuthorProfileAsync(subject, cancellationToken);

        var correction = VitalSigns.CreateCorrection(
            original, appointment.Id, subject.UserId!.Value, doctorId, displayName,
            command.Measurements, command.RecordedAtUtc ?? now, now);

        await _store.AddAsync(correction, cancellationToken);
        _notifier.EnqueueVitalSignsEvent(correction, appointment, "Created", now, _correlationIdProvider.CorrelationId);
        EnqueueAudit(subject.UserId, "ClinicalRecords.VitalSignsCorrected", correction.Id, now, new { correctsId = original.Id });
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Map(correction);
    }

    private async Task<Domain.Appointments.Appointment> LoadAndGateStatusAsync(Guid appointmentId, CancellationToken cancellationToken)
    {
        var appointment = await _appointmentStore.GetTrackedByIdAsync(appointmentId, cancellationToken)
            ?? throw new NotFoundException("Appointment not found.");

        // Gate de estado igual que notas: solo InProgress/Completed
        if (appointment.Status != Domain.Appointments.AppointmentStatus.InProgress
            && appointment.Status != Domain.Appointments.AppointmentStatus.Completed)
        {
            throw new ConflictException("Vital signs can only be recorded while the appointment is InProgress or Completed.");
        }

        return appointment;
    }

    private async Task<(VitalSigns VitalSigns, Domain.Appointments.Appointment Appointment)> LoadVitalSignsAndAppointmentAsync(Guid id, CancellationToken cancellationToken)
    {
        var vitalSigns = await _store.GetTrackedByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException("Vital-sign record not found.");
        var appointment = await _appointmentStore.GetTrackedByIdAsync(vitalSigns.AppointmentId, cancellationToken)
            ?? throw new NotFoundException("Appointment not found.");
        return (vitalSigns, appointment);
    }

    private async Task<(Guid DoctorId, string DisplayName)> ResolveAuthorProfileAsync(
        ClinicalRecordsAccessSubject subject,
        CancellationToken cancellationToken)
    {
        // Autoría = perfil de MÉDICO resuelto en runtime (confirmado Fase 8). Pendiente: autoría no-médica (enfermería/MA).
        var doctorId = await _doctorStore.GetIdByUserIdAsync(subject.UserId!.Value, cancellationToken);
        if (doctorId is null)
        {
            throw new ConflictException("Recording vital signs requires a doctor profile linked to your account.");
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
            userId, action, nameof(VitalSigns), entityId, now,
            _requestMetadata.IpAddress, _requestMetadata.UserAgent, null,
            newValues is null ? null : System.Text.Json.JsonSerializer.Serialize(newValues, JsonDefaults.Options),
            _correlationIdProvider.CorrelationId));
    }

    private static VitalSignsDto Map(VitalSigns v) => new(
        v.Id, v.AppointmentId, v.AuthorUserId, v.AuthorDoctorId, v.AuthorDisplayName,
        v.SystolicmmHg, v.DiastolicmmHg, v.HeartRateBpm, v.TemperatureC, v.TemperatureMethod, v.Position,
        v.RespiratoryRatePerMin, v.SpO2Percent, v.WeightKg, v.HeightCm, v.PainScore0to10, v.Note,
        v.IsFinal, v.RecordedAtUtc, v.FinalizedAt, v.FinalizedByUserId, v.CorrectsVitalSignsId,
        null, v.CreatedAt);
}