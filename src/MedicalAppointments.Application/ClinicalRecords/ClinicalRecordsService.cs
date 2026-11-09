using FluentValidation;
using MedicalAppointments.Application.Appointments;
using MedicalAppointments.Application.ClinicalRecords.Auth;
using MedicalAppointments.Application.ClinicalRecords.Common;
using MedicalAppointments.Application.ClinicalRecords.Outbox;
using MedicalAppointments.Application.ClinicalRecords.Stores;
using MedicalAppointments.Application.Common;
using MedicalAppointments.Application.Common.Audit;
using MedicalAppointments.Application.Common.Exceptions;
using MedicalAppointments.Application.MedicalStructure.Doctors;
using MedicalAppointments.Domain.Appointments;
using MedicalAppointments.Domain.ClinicalRecords;

namespace MedicalAppointments.Application.ClinicalRecords;

public sealed class ClinicalRecordsService : IClinicalRecordsService
{
    private readonly IValidator<CreateClinicalNoteCommand> _createValidator;
    private readonly IValidator<UpdateClinicalNoteCommand> _updateValidator;
    private readonly IValidator<CorrectClinicalNoteCommand> _correctValidator;

    private readonly IClinicalRecordsAccessService _access;
    private readonly IClinicalNoteStore _store;
    private readonly IAppointmentStore _appointmentStore;
    private readonly IDoctorStore _doctorStore;

    private readonly IUnitOfWork _unitOfWork;
    private readonly IClinicalRecordsEventNotifier _notifier;
    private readonly IAuditWriter _auditWriter;
    private readonly IRequestMetadataProvider _requestMetadata;
    private readonly ICorrelationIdProvider _correlationIdProvider;
    private readonly TimeProvider _timeProvider;

    public ClinicalRecordsService(
        IValidator<CreateClinicalNoteCommand> createValidator,
        IValidator<UpdateClinicalNoteCommand> updateValidator,
        IValidator<CorrectClinicalNoteCommand> correctValidator,
        IClinicalRecordsAccessService access,
        IClinicalNoteStore store,
        IAppointmentStore appointmentStore,
        IDoctorStore doctorStore,
        IUnitOfWork unitOfWork,
        IClinicalRecordsEventNotifier notifier,
        IAuditWriter auditWriter,
        IRequestMetadataProvider requestMetadata,
        ICorrelationIdProvider correlationIdProvider,
        TimeProvider timeProvider)
    {
        _createValidator = createValidator;
        _updateValidator = updateValidator;
        _correctValidator = correctValidator;
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

    public async Task<IReadOnlyList<ClinicalNoteDto>> GetNotesByAppointmentAsync(
        Guid appointmentId,
        CancellationToken cancellationToken = default)
    {
        var subject = await _access.GetSubjectAsync(cancellationToken);

        if (!_access.CanReadNotes(subject))
        {
            throw new ForbiddenException("You do not have access to clinical records.");
        }

        var appointment = await _appointmentStore.GetTrackedByIdAsync(appointmentId, cancellationToken)
            ?? throw new NotFoundException("Appointment not found.");

        if (!_access.CanReadNotesForAppointment(appointment, subject))
        {
            throw new ForbiddenException("You do not have access to this appointment's clinical records.");
        }

        var notes = await _store.GetByAppointmentAsync(appointmentId, cancellationToken);

        return notes.Select(Map).ToList();
    }

    public async Task<ClinicalNoteDto> CreateNoteAsync(
        CreateClinicalNoteCommand command,
        CancellationToken cancellationToken = default)
    {
        await _createValidator.ValidateAndThrowAsync(command, cancellationToken);

        var now = _timeProvider.GetUtcNow();
        var subject = await _access.GetSubjectAsync(cancellationToken);

        var appointment = await _appointmentStore.GetWithRelationsAsync(command.AppointmentId, cancellationToken)
            ?? throw new NotFoundException("Appointment not found.");

        if (appointment.Status is not (AppointmentStatus.InProgress or AppointmentStatus.Completed))
        {
            throw new ConflictException("Clinical notes can only be recorded for appointments in progress or completed.");
        }

        if (!_access.CanCreateForAppointment(appointment, subject))
        {
            throw new ForbiddenException("You do not have permission to author a clinical note for this appointment.");
        }

        if (!subject.DoctorId.HasValue || !subject.UserId.HasValue)
        {
            throw new ConflictException("The author must have a doctor profile and an authenticated user.");
        }

        var authorDoctor = await _doctorStore.GetTrackedWithRelationsAsync(subject.DoctorId.Value, cancellationToken)
            ?? throw new ConflictException("Author doctor profile not found.");

        var occurredAt = command.OccurredAtUtc ?? now;

        var note = ClinicalNote.Create(
            appointment.Id,
            subject.UserId.Value,
            subject.DoctorId.Value,
            $"{authorDoctor.FirstName} {authorDoctor.LastName}",
            command.Category,
            command.Title,
            command.Content,
            occurredAt,
            now);

        await _store.AddAsync(note, cancellationToken);

        _notifier.EnqueueNoteEvent(note, appointment, "Created", now, _correlationIdProvider.CorrelationId);
        EnqueueAudit(subject.UserId, "ClinicalRecords.NoteCreated", note.Id, now,
            new { appointmentId = appointment.Id, category = note.Category.ToString(), isFinal = false });

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Map(note);
    }

    public async Task<ClinicalNoteDto> UpdateNoteAsync(
        UpdateClinicalNoteCommand command,
        CancellationToken cancellationToken = default)
    {
        await _updateValidator.ValidateAndThrowAsync(command, cancellationToken);

        var now = _timeProvider.GetUtcNow();
        var subject = await _access.GetSubjectAsync(cancellationToken);

        var note = await _store.GetTrackedByIdAsync(command.NoteId, cancellationToken)
            ?? throw new NotFoundException("Clinical note not found.");

        if (!_access.CanUpdateNote(note, subject))
        {
            throw new ForbiddenException("You do not have permission to edit this note.");
        }

        note.UpdateDraft(command.Category, command.Title, command.Content, now);

        var appointment = await _appointmentStore.GetTrackedByIdAsync(note.AppointmentId, cancellationToken)
            ?? throw new NotFoundException("Appointment not found.");

        _notifier.EnqueueNoteEvent(note, appointment, "Updated", now, _correlationIdProvider.CorrelationId);
        EnqueueAudit(subject.UserId, "ClinicalRecords.NoteUpdated", note.Id, now,
            new { category = note.Category.ToString() });

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Map(note);
    }

    public async Task<ClinicalNoteDto> FinalizeNoteAsync(
        FinalizeClinicalNoteCommand command,
        CancellationToken cancellationToken = default)
    {
        var now = _timeProvider.GetUtcNow();
        var subject = await _access.GetSubjectAsync(cancellationToken);

        var note = await _store.GetTrackedByIdAsync(command.NoteId, cancellationToken)
            ?? throw new NotFoundException("Clinical note not found.");

        if (!_access.CanFinalizeNote(note, subject))
        {
            throw new ForbiddenException("You do not have permission to finalize this note.");
        }

        if (!subject.UserId.HasValue)
        {
            throw new ConflictException("An authenticated user is required to finalize a note.");
        }

        note.Finalize(subject.UserId.Value, now);

        var appointment = await _appointmentStore.GetTrackedByIdAsync(note.AppointmentId, cancellationToken)
            ?? throw new NotFoundException("Appointment not found.");

        _notifier.EnqueueNoteEvent(note, appointment, "Finalized", now, _correlationIdProvider.CorrelationId);
        EnqueueAudit(subject.UserId, "ClinicalRecords.NoteFinalized", note.Id, now, null);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Map(note);
    }

    public async Task<ClinicalNoteDto> CorrectNoteAsync(
        CorrectClinicalNoteCommand command,
        CancellationToken cancellationToken = default)
    {
        await _correctValidator.ValidateAndThrowAsync(command, cancellationToken);

        var now = _timeProvider.GetUtcNow();
        var subject = await _access.GetSubjectAsync(cancellationToken);

        var original = await _store.GetTrackedByIdAsync(command.NoteId, cancellationToken)
            ?? throw new NotFoundException("Clinical note not found.");

        if (!_access.CanCorrectNote(original, subject))
        {
            throw new ForbiddenException("You do not have permission to correct this note.");
        }

        if (!original.IsFinal)
        {
            throw new ConflictException("Only a finalized note can be corrected.");
        }

        var appointment = await _appointmentStore.GetWithRelationsAsync(original.AppointmentId, cancellationToken)
            ?? throw new NotFoundException("Appointment not found.");

        if (appointment.Status is not (AppointmentStatus.InProgress or AppointmentStatus.Completed))
        {
            throw new ConflictException("Clinical notes can only be recorded for appointments in progress or completed.");
        }

        if (!_access.CanCreateForAppointment(appointment, subject))
        {
            throw new ForbiddenException("You do not have permission to author a correction for this appointment.");
        }

        if (!subject.DoctorId.HasValue || !subject.UserId.HasValue)
        {
            throw new ConflictException("The author must have a doctor profile and an authenticated user.");
        }

        var authorDoctor = await _doctorStore.GetTrackedWithRelationsAsync(subject.DoctorId.Value, cancellationToken)
            ?? throw new ConflictException("Author doctor profile not found.");

        var occurredAt = command.OccurredAtUtc ?? now;

        var correction = ClinicalNote.CreateCorrection(
            original,
            subject.UserId.Value,
            subject.DoctorId.Value,
            $"{authorDoctor.FirstName} {authorDoctor.LastName}",
            command.Category,
            command.Title,
            command.Content,
            occurredAt,
            now);

        await _store.AddAsync(correction, cancellationToken);

        _notifier.EnqueueNoteEvent(correction, appointment, "Created", now, _correlationIdProvider.CorrelationId);
        EnqueueAudit(subject.UserId, "ClinicalRecords.NoteCorrected", correction.Id, now,
            new { correctsNoteId = original.Id });

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Map(correction);
    }

    public async Task<IReadOnlyList<ClinicalHistoryItemDto>> GetPatientHistoryAsync(
    Guid patientId,
    CancellationToken cancellationToken = default)
    {
        var subject = await _access.GetSubjectAsync(cancellationToken);

        if (!_access.CanReadNotes(subject))
        {
            throw new ForbiddenException("You do not have access to clinical records.");
        }

        // read_assigned: solo notas de citas del propio médico del sujeto. read_all: sin filtro.
        var doctorIdFilter = subject.CanReadAll ? (Guid?)null : subject.DoctorId;

        var notes = await _store.GetHistoryByPatientAsync(patientId, doctorIdFilter, cancellationToken);

        var result = new List<ClinicalHistoryItemDto>(notes.Count);

        foreach (var note in notes)
        {
            var appointment = note.Appointment; // Include en cascada ya trajo la cita
            result.Add(new ClinicalHistoryItemDto(
                note.Id,
                appointment.Id,
                appointment.LocalDate,
                appointment.StartAt,
                appointment.Doctor is null ? string.Empty : $"{appointment.Doctor.FirstName} {appointment.Doctor.LastName}",
                appointment.Clinic?.Name ?? string.Empty,
                note.Category,
                note.Title,
                note.Content,
                note.IsFinal,
                note.AuthorDisplayName,
                note.OccurredAtUtc,
                note.FinalizedAt,
                note.CorrectsNoteId,
                0)); // attachment count se rellena abajo si hace falta; 8b lo deja en 0 (deuda 8c: badge de adjuntos en timeline)
        }

        return result;
    }

    private void EnqueueAudit(Guid? userId, string action, Guid entityId, DateTimeOffset now, object? newValues)
    {
        _auditWriter.Enqueue(new AuditEntry(
            userId, action, nameof(ClinicalNote), entityId, now,
            _requestMetadata.IpAddress, _requestMetadata.UserAgent, null,
            newValues is null ? null : System.Text.Json.JsonSerializer.Serialize(newValues, JsonDefaults.Options),
            _correlationIdProvider.CorrelationId));
    }

    private static ClinicalNoteDto Map(ClinicalNote n) => new(
        n.Id,
        n.AppointmentId,
        n.AuthorUserId,
        n.AuthorDoctorId,
        n.AuthorDisplayName,
        n.Category,
        n.Title,
        n.Content,
        n.IsFinal,
        n.OccurredAtUtc,
        n.FinalizedAt,
        n.FinalizedByUserId,
        n.CorrectsNoteId,
        n.CorrectsNote?.Title,
        n.CreatedAt,
        n.UpdatedAt);
}