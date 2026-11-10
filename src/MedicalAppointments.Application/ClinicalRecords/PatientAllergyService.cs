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

public sealed class PatientAllergyService : IPatientAllergyService
{
    private readonly IClinicalRecordsAccessService _access;
    private readonly IPatientAllergyStore _store;
    private readonly IDoctorStore _doctorStore;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClinicalRecordsEventNotifier _notifier;
    private readonly IAuditWriter _auditWriter;
    private readonly IRequestMetadataProvider _requestMetadata;
    private readonly ICorrelationIdProvider _correlationIdProvider;
    private readonly TimeProvider _timeProvider;

    public PatientAllergyService(
        IClinicalRecordsAccessService access,
        IPatientAllergyStore store,
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
        _doctorStore = doctorStore;
        _unitOfWork = unitOfWork;
        _notifier = notifier;
        _auditWriter = auditWriter;
        _requestMetadata = requestMetadata;
        _correlationIdProvider = correlationIdProvider;
        _timeProvider = timeProvider;
    }

    public async Task<IReadOnlyList<PatientAllergyDto>> ListByPatientAsync(Guid patientId, CancellationToken cancellationToken = default)
    {
        var subject = await _access.GetSubjectAsync(cancellationToken);
        if (!subject.CanReadAll && !subject.CanReadAssigned)
        {
            throw new ForbiddenException("You do not have access to clinical records.");
        }
        // Sin filtro de scope-paciente (pending decision; ver notas de diseño).
        if (!await _store.PatientExistsAsync(patientId, cancellationToken))
        {
            throw new NotFoundException("Patient not found.");
        }

        var items = await _store.GetByPatientAsync(patientId, cancellationToken);
        return items.Select(Map).ToList();
    }

    public async Task<PatientAllergyDto> CreateAsync(CreateAllergyCommand command, CancellationToken cancellationToken = default)
    {
        var now = _timeProvider.GetUtcNow();
        var subject = await _access.GetSubjectAsync(cancellationToken);

        if (!subject.CanCreate) throw new ForbiddenException("You do not have permission to record allergies.");
        if (!await _store.PatientExistsAsync(command.PatientId, cancellationToken)) throw new NotFoundException("Patient not found.");

        var (doctorId, displayName) = await ResolveAuthorProfileAsync(subject, cancellationToken);

        var allergy = PatientAllergy.Create(
            command.PatientId, subject.UserId!.Value, doctorId, displayName,
            command.Allergen, command.AllergenClass, command.Severity, command.Reaction, command.OnsetDate,
            command.RecordedAtUtc ?? now, now);

        await _store.AddAsync(allergy, cancellationToken);
        _notifier.EnqueueAllergyEvent(allergy, "Created", now, _correlationIdProvider.CorrelationId);
        EnqueueAudit(subject.UserId, "ClinicalRecords.AllergyCreated", allergy.Id, now, new { patientId = allergy.PatientId });
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Map(allergy);
    }

    public async Task<PatientAllergyDto> UpdateAsync(UpdateAllergyCommand command, CancellationToken cancellationToken = default)
    {
        var now = _timeProvider.GetUtcNow();
        var subject = await _access.GetSubjectAsync(cancellationToken);
        var allergy = await LoadAsync(command.Id, cancellationToken);

        if (!subject.CanUpdate || !(subject.CanManageAll || allergy.AuthorUserId == subject.UserId))
            throw new ForbiddenException("You do not have permission to edit this allergy.");

        allergy.UpdateDraft(command.Allergen, command.AllergenClass, command.Severity, command.Reaction, command.OnsetDate, now);
        _notifier.EnqueueAllergyEvent(allergy, "Updated", now, _correlationIdProvider.CorrelationId);
        EnqueueAudit(subject.UserId, "ClinicalRecords.AllergyUpdated", allergy.Id, now, null);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Map(allergy);
    }

    public async Task<PatientAllergyDto> FinalizeAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var now = _timeProvider.GetUtcNow();
        var subject = await _access.GetSubjectAsync(cancellationToken);
        var allergy = await LoadAsync(id, cancellationToken);

        if (!subject.CanFinalize || !(subject.CanManageAll || allergy.AuthorUserId == subject.UserId))
            throw new ForbiddenException("You do not have permission to finalize this allergy.");

        allergy.Finalize(subject.UserId!.Value, now);
        _notifier.EnqueueAllergyEvent(allergy, "Finalized", now, _correlationIdProvider.CorrelationId);
        EnqueueAudit(subject.UserId, "ClinicalRecords.AllergyFinalized", allergy.Id, now, null);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Map(allergy);
    }

    public async Task<PatientAllergyDto> ResolveAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var now = _timeProvider.GetUtcNow();
        var subject = await _access.GetSubjectAsync(cancellationToken);
        var allergy = await LoadAsync(id, cancellationToken);

        if (!subject.CanUpdate || !(subject.CanManageAll || allergy.AuthorUserId == subject.UserId))
            throw new ForbiddenException("You do not have permission to resolve this allergy.");

        allergy.MarkResolved(subject.UserId!.Value, now);
        _notifier.EnqueueAllergyEvent(allergy, "Resolved", now, _correlationIdProvider.CorrelationId);
        EnqueueAudit(subject.UserId, "ClinicalRecords.AllergyResolved", allergy.Id, now, null);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Map(allergy);
    }

    public async Task<PatientAllergyDto> UnresolveAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var now = _timeProvider.GetUtcNow();
        var subject = await _access.GetSubjectAsync(cancellationToken);
        var allergy = await LoadAsync(id, cancellationToken);

        if (!subject.CanUpdate || !(subject.CanManageAll || allergy.AuthorUserId == subject.UserId))
            throw new ForbiddenException("You do not have permission to reactivate this allergy.");

        allergy.MarkUnresolved(subject.UserId!.Value, now);
        _notifier.EnqueueAllergyEvent(allergy, "Unresolved", now, _correlationIdProvider.CorrelationId);
        EnqueueAudit(subject.UserId, "ClinicalRecords.AllergyUnresolved", allergy.Id, now, null);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Map(allergy);
    }

    public async Task<PatientAllergyDto> CorrectAsync(CorrectAllergyCommand command, CancellationToken cancellationToken = default)
    {
        var now = _timeProvider.GetUtcNow();
        var subject = await _access.GetSubjectAsync(cancellationToken);
        var original = await LoadAsync(command.OriginalId, cancellationToken);

        if (!subject.CanCorrect || !(subject.CanManageAll || original.AuthorUserId == subject.UserId))
            throw new ForbiddenException("You do not have permission to correct this allergy.");

        if (!await _store.PatientExistsAsync(command.PatientId, cancellationToken)) throw new NotFoundException("Patient not found.");
        var (doctorId, displayName) = await ResolveAuthorProfileAsync(subject, cancellationToken);

        var correction = PatientAllergy.CreateCorrection(
            original, command.PatientId, subject.UserId!.Value, doctorId, displayName,
            command.Allergen, command.AllergenClass, command.Severity, command.Reaction, command.OnsetDate,
            command.RecordedAtUtc ?? now, now);

        await _store.AddAsync(correction, cancellationToken);
        _notifier.EnqueueAllergyEvent(correction, "Created", now, _correlationIdProvider.CorrelationId);
        EnqueueAudit(subject.UserId, "ClinicalRecords.AllergyCorrected", correction.Id, now, new { correctsId = original.Id });
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Map(correction);
    }

    private async Task<PatientAllergy> LoadAsync(Guid id, CancellationToken cancellationToken)
        => await _store.GetTrackedByIdAsync(id, cancellationToken) ?? throw new NotFoundException("Allergy not found.");

    private async Task<(Guid DoctorId, string DisplayName)> ResolveAuthorProfileAsync(
        ClinicalRecordsAccessSubject subject, CancellationToken cancellationToken)
    {
        // Reuso LITERAL el fix de 8d.1: GetIdByUserIdAsync + GetTrackedWithRelationsAsync (firmas de tu IDoctorStore).
        var doctorId = await _doctorStore.GetIdByUserIdAsync(subject.UserId!.Value, cancellationToken);
        if (doctorId is null) throw new ConflictException("Recording allergies requires a doctor profile linked to your account.");
        var doctor = await _doctorStore.GetTrackedWithRelationsAsync(doctorId.Value, cancellationToken);
        var displayName = doctor is null ? string.Empty : $"{doctor.FirstName} {doctor.LastName}".Trim();
        if (string.IsNullOrWhiteSpace(displayName)) throw new ConflictException("Author display name could not be resolved from the doctor profile.");
        return (doctorId.Value, displayName);
    }

    private void EnqueueAudit(Guid? userId, string action, Guid entityId, DateTimeOffset now, object? newValues)
    {
        _auditWriter.Enqueue(new AuditEntry(
            userId, action, nameof(PatientAllergy), entityId, now,
            _requestMetadata.IpAddress, _requestMetadata.UserAgent, null,
            newValues is null ? null : System.Text.Json.JsonSerializer.Serialize(newValues, JsonDefaults.Options),
            _correlationIdProvider.CorrelationId));
    }

    private static PatientAllergyDto Map(PatientAllergy a) => new(
        a.Id, a.PatientId, a.AuthorUserId, a.AuthorDoctorId, a.AuthorDisplayName,
        a.Allergen, a.AllergenClass, a.Severity, a.Reaction, a.OnsetDate,
        a.IsResolved, a.ResolvedAt, a.ResolvedByUserId,
        a.IsFinal, a.RecordedAtUtc, a.FinalizedAt, a.FinalizedByUserId, a.CorrectsAllergyId,
        null, a.CreatedAt);
}