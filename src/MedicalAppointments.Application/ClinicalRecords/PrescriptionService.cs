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

public sealed class PrescriptionService : IPrescriptionService
{
    private readonly IPrescriptionsAccessService _access;
    private readonly IPrescriptionStore _store;
    private readonly IDoctorStore _doctorStore;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClinicalRecordsEventNotifier _notifier;
    private readonly IAuditWriter _auditWriter;
    private readonly IRequestMetadataProvider _requestMetadata;
    private readonly ICorrelationIdProvider _correlationIdProvider;
    private readonly TimeProvider _timeProvider;

    public PrescriptionService(IPrescriptionsAccessService access, IPrescriptionStore store, IDoctorStore doctorStore,
        IUnitOfWork unitOfWork, IClinicalRecordsEventNotifier notifier, IAuditWriter auditWriter,
        IRequestMetadataProvider requestMetadata, ICorrelationIdProvider correlationIdProvider, TimeProvider timeProvider)
    { _access = access; _store = store; _doctorStore = doctorStore; _unitOfWork = unitOfWork; _notifier = notifier; _auditWriter = auditWriter; _requestMetadata = requestMetadata; _correlationIdProvider = correlationIdProvider; _timeProvider = timeProvider; }

    public async Task<IReadOnlyList<PrescriptionDto>> ListByPatientAsync(Guid patientId, CancellationToken cancellationToken = default)
    {
        var subject = await _access.GetSubjectAsync(cancellationToken);
        if (!_access.CanReadPrescriptions(subject)) throw new ForbiddenException("You do not have access to prescriptions.");
        if (!await _store.PatientExistsAsync(patientId, cancellationToken)) throw new NotFoundException("Patient not found.");
        if (_access.CanReadOperationalPrescriptions(subject) && !subject.CanManageAll && !subject.CanReadAll)
        {
            var operational = await _store.GetOperationalByPatientAsync(patientId, cancellationToken);
            return operational.Select(Map).ToList();
        }
        if (!subject.CanManageAll && !subject.CanReadAll &&
            (!subject.DoctorId.HasValue ||
             !await _store.HasAppointmentWithDoctorAsync(patientId, subject.DoctorId.Value, cancellationToken)))
            throw new ForbiddenException("You do not have access to this patient's prescriptions.");
        var items = await _store.GetByPatientAsync(patientId, cancellationToken);
        return items.Select(Map).ToList();
    }

    public async Task<PrescriptionDto> CreateAsync(CreatePrescriptionCommand command, CancellationToken cancellationToken = default)
    {
        var now = _timeProvider.GetUtcNow();
        var subject = await _access.GetSubjectAsync(cancellationToken);
        if (!_access.CanCreateForPatient(subject)) throw new ForbiddenException("You do not have permission to prescribe.");
        if (!await _store.PatientExistsAsync(command.PatientId, cancellationToken)) throw new NotFoundException("Patient not found.");
        var (doctorId, displayName) = await ResolveAuthorProfileAsync(subject, cancellationToken);
        var p = Prescription.Create(command.PatientId, subject.UserId!.Value, doctorId, displayName,
            command.MedicationName, command.Strength, command.DosageForm, command.Frequency, command.Route,
            command.Quantity, command.RefillsAllowed, command.DaysSupply, command.Instructions, command.Note,
            command.RecordedAtUtc ?? now, now);
        await _store.AddAsync(p, cancellationToken);
        _notifier.EnqueuePrescriptionEvent(p, "Created", now, _correlationIdProvider.CorrelationId);
        EnqueueAudit(subject.UserId, "ClinicalRecords.PrescriptionCreated", p.Id, now, new { patientId = p.PatientId });
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Map(p);
    }

    public async Task<PrescriptionDto> UpdateAsync(UpdatePrescriptionCommand command, CancellationToken cancellationToken = default)
    {
        var now = _timeProvider.GetUtcNow();
        var subject = await _access.GetSubjectAsync(cancellationToken);
        var p = await LoadAsync(command.Id, cancellationToken);
        if (!_access.CanUpdateDraft(p, subject)) throw new ForbiddenException("You do not have permission to edit this prescription.");
        p.UpdateDraft(command.MedicationName, command.Strength, command.DosageForm, command.Frequency, command.Route,
            command.Quantity, command.RefillsAllowed, command.DaysSupply, command.Instructions, command.Note, now);
        _notifier.EnqueuePrescriptionEvent(p, "Updated", now, _correlationIdProvider.CorrelationId);
        EnqueueAudit(subject.UserId, "ClinicalRecords.PrescriptionUpdated", p.Id, now, null);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Map(p);
    }

    public async Task<PrescriptionDto> IssueAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var now = _timeProvider.GetUtcNow();
        var subject = await _access.GetSubjectAsync(cancellationToken);
        var p = await LoadAsync(id, cancellationToken);
        if (!_access.CanIssue(p, subject)) throw new ForbiddenException("You do not have permission to issue this prescription.");
        p.Issue(subject.UserId!.Value, now);
        _notifier.EnqueuePrescriptionEvent(p, "Issued", now, _correlationIdProvider.CorrelationId);
        EnqueueAudit(subject.UserId, "ClinicalRecords.PrescriptionIssued", p.Id, now, null);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Map(p);
    }

    public async Task<PrescriptionDto> CorrectAsync(CorrectPrescriptionCommand command, CancellationToken cancellationToken = default)
    {
        var now = _timeProvider.GetUtcNow();
        var subject = await _access.GetSubjectAsync(cancellationToken);
        var original = await LoadAsync(command.OriginalId, cancellationToken);
        if (!_access.CanCorrect(original, subject)) throw new ForbiddenException("You do not have permission to correct this prescription.");
        if (!await _store.PatientExistsAsync(command.PatientId, cancellationToken)) throw new NotFoundException("Patient not found.");
        var (doctorId, displayName) = await ResolveAuthorProfileAsync(subject, cancellationToken);
        var correction = Prescription.CreateCorrection(original, command.PatientId, subject.UserId!.Value, doctorId, displayName,
            command.MedicationName, command.Strength, command.DosageForm, command.Frequency, command.Route,
            command.Quantity, command.RefillsAllowed, command.DaysSupply, command.Instructions, command.Note,
            command.RecordedAtUtc ?? now, now);
        await _store.AddAsync(correction, cancellationToken);
        _notifier.EnqueuePrescriptionEvent(correction, "Corrected", now, _correlationIdProvider.CorrelationId);
        EnqueueAudit(subject.UserId, "ClinicalRecords.PrescriptionCorrected", correction.Id, now, new { correctsId = original.Id });
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Map(correction);
    }

    public async Task<PrescriptionDto> RenewAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var now = _timeProvider.GetUtcNow();
        var subject = await _access.GetSubjectAsync(cancellationToken);
        var original = await LoadAsync(id, cancellationToken);
        if (!_access.CanRenew(original, subject)) throw new ForbiddenException("You do not have permission to renew this prescription.");
        var (doctorId, displayName) = await ResolveAuthorProfileAsync(subject, cancellationToken);
        var renewal = Prescription.Renew(original, subject.UserId!.Value, doctorId, displayName, now);
        await _store.AddAsync(renewal, cancellationToken);
        _notifier.EnqueuePrescriptionEvent(renewal, "Renewed", now, _correlationIdProvider.CorrelationId);
        EnqueueAudit(subject.UserId, "ClinicalRecords.PrescriptionRenewed", renewal.Id, now, new { renewedFromId = original.Id });
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Map(renewal);
    }

    public async Task<PrescriptionDto> CancelAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var now = _timeProvider.GetUtcNow();
        var subject = await _access.GetSubjectAsync(cancellationToken);
        var p = await LoadAsync(id, cancellationToken);
        if (!_access.CanCancel(p, subject)) throw new ForbiddenException("You do not have permission to cancel this prescription.");
        p.Cancel(subject.UserId!.Value, now);
        _notifier.EnqueuePrescriptionEvent(p, "Cancelled", now, _correlationIdProvider.CorrelationId);
        EnqueueAudit(subject.UserId, "ClinicalRecords.PrescriptionCancelled", p.Id, now, null);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Map(p);
    }

    public async Task<PrescriptionDto> DispenseAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var now = _timeProvider.GetUtcNow();
        var subject = await _access.GetSubjectAsync(cancellationToken);
        var p = await LoadAsync(id, cancellationToken);
        if (!_access.CanDispense(p, subject)) throw new ForbiddenException("You do not have permission to dispense this prescription.");
        p.Dispense(subject.UserId!.Value, now);
        _notifier.EnqueuePrescriptionEvent(p, "Dispensed", now, _correlationIdProvider.CorrelationId);
        EnqueueAudit(subject.UserId, "ClinicalRecords.PrescriptionDispensed", p.Id, now, new { patientId = p.PatientId });
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Map(p);
    }

    public async Task<PrescriptionDto> ExpireAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var now = _timeProvider.GetUtcNow();
        var subject = await _access.GetSubjectAsync(cancellationToken);
        var p = await LoadAsync(id, cancellationToken);
        if (!_access.CanExpire(p, subject)) throw new ForbiddenException("You do not have permission to expire this prescription.");
        p.Expire(now);
        _notifier.EnqueuePrescriptionEvent(p, "Expired", now, _correlationIdProvider.CorrelationId);
        EnqueueAudit(subject.UserId, "ClinicalRecords.PrescriptionExpired", p.Id, now, new { patientId = p.PatientId });
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Map(p);
    }

    private async Task<Prescription> LoadAsync(Guid id, CancellationToken cancellationToken)
        => await _store.GetTrackedByIdAsync(id, cancellationToken) ?? throw new NotFoundException("Prescription not found.");

    private async Task<(Guid DoctorId, string DisplayName)> ResolveAuthorProfileAsync(PrescriptionsAccessSubject subject, CancellationToken cancellationToken)
    {
        var doctorId = await _doctorStore.GetIdByUserIdAsync(subject.UserId!.Value, cancellationToken);
        if (doctorId is null) throw new ConflictException("Prescribing requires a doctor profile linked to your account.");
        var doctor = await _doctorStore.GetTrackedWithRelationsAsync(doctorId.Value, cancellationToken);
        var displayName = doctor is null ? string.Empty : $"{doctor.FirstName} {doctor.LastName}".Trim();
        if (string.IsNullOrWhiteSpace(displayName)) throw new ConflictException("Author display name could not be resolved from the doctor profile.");
        return (doctorId.Value, displayName);
    }

    private void EnqueueAudit(Guid? userId, string action, Guid entityId, DateTimeOffset now, object? newValues)
    {
        _auditWriter.Enqueue(new AuditEntry(userId, action, nameof(Prescription), entityId, now,
            _requestMetadata.IpAddress, _requestMetadata.UserAgent, null,
            newValues is null ? null : System.Text.Json.JsonSerializer.Serialize(newValues, JsonDefaults.Options),
            _correlationIdProvider.CorrelationId));
    }

    private static PrescriptionDto Map(Prescription p) => new(
        p.Id, p.PatientId, p.AuthorUserId, p.AuthorDoctorId, p.AuthorDisplayName,
        p.MedicationName, p.Strength, p.DosageForm, p.Frequency, p.Route, p.Quantity, p.RefillsAllowed, p.DaysSupply,
        p.Instructions, p.Note, p.Status, p.IsFinal, p.RecordedAtUtc, p.IssuedAt, p.IssuedByUserId,
        p.CancelledAt, p.CancelledByUserId, p.CorrectsPrescriptionId, null, p.RenewedFromPrescriptionId, p.CreatedAt);
}
