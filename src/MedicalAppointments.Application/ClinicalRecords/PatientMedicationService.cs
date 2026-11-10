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

public sealed class PatientMedicationService : IPatientMedicationService
{
    private readonly IClinicalRecordsAccessService _access;
    private readonly IPatientMedicationStore _store;
    private readonly IDoctorStore _doctorStore;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClinicalRecordsEventNotifier _notifier;
    private readonly IAuditWriter _auditWriter;
    private readonly IRequestMetadataProvider _requestMetadata;
    private readonly ICorrelationIdProvider _correlationIdProvider;
    private readonly TimeProvider _timeProvider;

    public PatientMedicationService(
        IClinicalRecordsAccessService access, IPatientMedicationStore store, IDoctorStore doctorStore,
        IUnitOfWork unitOfWork, IClinicalRecordsEventNotifier notifier, IAuditWriter auditWriter,
        IRequestMetadataProvider requestMetadata, ICorrelationIdProvider correlationIdProvider, TimeProvider timeProvider)
    {
        _access = access; _store = store; _doctorStore = doctorStore; _unitOfWork = unitOfWork;
        _notifier = notifier; _auditWriter = auditWriter; _requestMetadata = requestMetadata;
        _correlationIdProvider = correlationIdProvider; _timeProvider = timeProvider;
    }

    public async Task<IReadOnlyList<PatientMedicationDto>> ListByPatientAsync(Guid patientId, CancellationToken cancellationToken = default)
    {
        var subject = await _access.GetSubjectAsync(cancellationToken);
        if (!subject.CanReadAll && !subject.CanReadAssigned) throw new ForbiddenException("You do not have access to clinical records.");
        if (!await _store.PatientExistsAsync(patientId, cancellationToken)) throw new NotFoundException("Patient not found.");
        var items = await _store.GetByPatientAsync(patientId, cancellationToken);
        return items.Select(Map).ToList();
    }

    public async Task<PatientMedicationDto> CreateAsync(CreateMedicationCommand command, CancellationToken cancellationToken = default)
    {
        var now = _timeProvider.GetUtcNow();
        var subject = await _access.GetSubjectAsync(cancellationToken);
        if (!subject.CanCreate) throw new ForbiddenException("You do not have permission to record medications.");
        if (!await _store.PatientExistsAsync(command.PatientId, cancellationToken)) throw new NotFoundException("Patient not found.");
        var (doctorId, displayName) = await ResolveAuthorProfileAsync(subject, cancellationToken);
        var medication = PatientMedication.Create(command.PatientId, subject.UserId!.Value, doctorId, displayName,
            command.MedicationName, command.Strength, command.DosageForm, command.Frequency, command.Route,
            command.StartDate, command.Indication, command.RecordedAtUtc ?? now, now);
        await _store.AddAsync(medication, cancellationToken);
        _notifier.EnqueueMedicationEvent(medication, "Created", now, _correlationIdProvider.CorrelationId);
        EnqueueAudit(subject.UserId, "ClinicalRecords.MedicationCreated", medication.Id, now, new { patientId = medication.PatientId });
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Map(medication);
    }

    public async Task<PatientMedicationDto> UpdateAsync(UpdateMedicationCommand command, CancellationToken cancellationToken = default)
    {
        var now = _timeProvider.GetUtcNow();
        var subject = await _access.GetSubjectAsync(cancellationToken);
        var m = await LoadAsync(command.Id, cancellationToken);
        if (!subject.CanUpdate || !(subject.CanManageAll || m.AuthorUserId == subject.UserId)) throw new ForbiddenException("You do not have permission to edit this medication.");
        m.UpdateDraft(command.MedicationName, command.Strength, command.DosageForm, command.Frequency, command.Route, command.StartDate, command.Indication, now);
        _notifier.EnqueueMedicationEvent(m, "Updated", now, _correlationIdProvider.CorrelationId);
        EnqueueAudit(subject.UserId, "ClinicalRecords.MedicationUpdated", m.Id, now, null);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Map(m);
    }

    public async Task<PatientMedicationDto> FinalizeAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var now = _timeProvider.GetUtcNow();
        var subject = await _access.GetSubjectAsync(cancellationToken);
        var m = await LoadAsync(id, cancellationToken);
        if (!subject.CanFinalize || !(subject.CanManageAll || m.AuthorUserId == subject.UserId)) throw new ForbiddenException("You do not have permission to finalize this medication.");
        m.Finalize(subject.UserId!.Value, now);
        _notifier.EnqueueMedicationEvent(m, "Finalized", now, _correlationIdProvider.CorrelationId);
        EnqueueAudit(subject.UserId, "ClinicalRecords.MedicationFinalized", m.Id, now, null);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Map(m);
    }

    public async Task<PatientMedicationDto> SuspendAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var now = _timeProvider.GetUtcNow();
        var subject = await _access.GetSubjectAsync(cancellationToken);
        var m = await LoadAsync(id, cancellationToken);
        if (!subject.CanUpdate || !(subject.CanManageAll || m.AuthorUserId == subject.UserId)) throw new ForbiddenException("You do not have permission to suspend this medication.");
        m.Suspend(subject.UserId!.Value, now);
        _notifier.EnqueueMedicationEvent(m, "StatusChanged", now, _correlationIdProvider.CorrelationId);
        EnqueueAudit(subject.UserId, "ClinicalRecords.MedicationSuspended", m.Id, now, null);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Map(m);
    }

    public async Task<PatientMedicationDto> ResumeAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var now = _timeProvider.GetUtcNow();
        var subject = await _access.GetSubjectAsync(cancellationToken);
        var m = await LoadAsync(id, cancellationToken);
        if (!subject.CanUpdate || !(subject.CanManageAll || m.AuthorUserId == subject.UserId)) throw new ForbiddenException("You do not have permission to resume this medication.");
        m.Resume(subject.UserId!.Value, now);
        _notifier.EnqueueMedicationEvent(m, "StatusChanged", now, _correlationIdProvider.CorrelationId);
        EnqueueAudit(subject.UserId, "ClinicalRecords.MedicationResumed", m.Id, now, null);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Map(m);
    }

    public async Task<PatientMedicationDto> DiscontinueAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var now = _timeProvider.GetUtcNow();
        var subject = await _access.GetSubjectAsync(cancellationToken);
        var m = await LoadAsync(id, cancellationToken);
        if (!subject.CanUpdate || !(subject.CanManageAll || m.AuthorUserId == subject.UserId)) throw new ForbiddenException("You do not have permission to discontinue this medication.");
        m.Discontinue(subject.UserId!.Value, now);
        _notifier.EnqueueMedicationEvent(m, "StatusChanged", now, _correlationIdProvider.CorrelationId);
        EnqueueAudit(subject.UserId, "ClinicalRecords.MedicationDiscontinued", m.Id, now, null);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Map(m);
    }

    public async Task<PatientMedicationDto> CorrectAsync(CorrectMedicationCommand command, CancellationToken cancellationToken = default)
    {
        var now = _timeProvider.GetUtcNow();
        var subject = await _access.GetSubjectAsync(cancellationToken);
        var original = await LoadAsync(command.OriginalId, cancellationToken);
        if (!subject.CanCorrect || !(subject.CanManageAll || original.AuthorUserId == subject.UserId)) throw new ForbiddenException("You do not have permission to correct this medication.");
        if (!await _store.PatientExistsAsync(command.PatientId, cancellationToken)) throw new NotFoundException("Patient not found.");
        var (doctorId, displayName) = await ResolveAuthorProfileAsync(subject, cancellationToken);
        var correction = PatientMedication.CreateCorrection(original, command.PatientId, subject.UserId!.Value, doctorId, displayName,
            command.MedicationName, command.Strength, command.DosageForm, command.Frequency, command.Route,
            command.StartDate, command.Indication, command.RecordedAtUtc ?? now, now);
        await _store.AddAsync(correction, cancellationToken);
        _notifier.EnqueueMedicationEvent(correction, "Created", now, _correlationIdProvider.CorrelationId);
        EnqueueAudit(subject.UserId, "ClinicalRecords.MedicationCorrected", correction.Id, now, new { correctsId = original.Id });
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Map(correction);
    }

    private async Task<PatientMedication> LoadAsync(Guid id, CancellationToken cancellationToken)
        => await _store.GetTrackedByIdAsync(id, cancellationToken) ?? throw new NotFoundException("Medication not found.");

    private async Task<(Guid DoctorId, string DisplayName)> ResolveAuthorProfileAsync(ClinicalRecordsAccessSubject subject, CancellationToken cancellationToken)
    {
        var doctorId = await _doctorStore.GetIdByUserIdAsync(subject.UserId!.Value, cancellationToken);
        if (doctorId is null) throw new ConflictException("Recording medications requires a doctor profile linked to your account.");
        var doctor = await _doctorStore.GetTrackedWithRelationsAsync(doctorId.Value, cancellationToken);
        var displayName = doctor is null ? string.Empty : $"{doctor.FirstName} {doctor.LastName}".Trim();
        if (string.IsNullOrWhiteSpace(displayName)) throw new ConflictException("Author display name could not be resolved from the doctor profile.");
        return (doctorId.Value, displayName);
    }

    private void EnqueueAudit(Guid? userId, string action, Guid entityId, DateTimeOffset now, object? newValues)
    {
        _auditWriter.Enqueue(new AuditEntry(userId, action, nameof(PatientMedication), entityId, now,
            _requestMetadata.IpAddress, _requestMetadata.UserAgent, null,
            newValues is null ? null : System.Text.Json.JsonSerializer.Serialize(newValues, JsonDefaults.Options),
            _correlationIdProvider.CorrelationId));
    }

    private static PatientMedicationDto Map(PatientMedication m) => new(
        m.Id, m.PatientId, m.AuthorUserId, m.AuthorDoctorId, m.AuthorDisplayName,
        m.MedicationName, m.Strength, m.DosageForm, m.Frequency, m.Route, m.StartDate, m.Indication,
        m.Status, m.SuspendedAt, m.DiscontinuedAt, m.StatusChangedByUserId,
        m.IsFinal, m.RecordedAtUtc, m.FinalizedAt, m.FinalizedByUserId, m.CorrectsMedicationId, null, m.CreatedAt);
}