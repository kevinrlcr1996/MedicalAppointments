using System.Security.Cryptography;
using MedicalAppointments.Application.Appointments;
using MedicalAppointments.Application.ClinicalRecords.Auth;
using MedicalAppointments.Application.ClinicalRecords.Common;
using MedicalAppointments.Application.ClinicalRecords.Outbox;
using MedicalAppointments.Application.ClinicalRecords.Storage;
using MedicalAppointments.Application.ClinicalRecords.Stores;
using MedicalAppointments.Application.Common;
using MedicalAppointments.Application.Common.Audit;
using MedicalAppointments.Application.Common.Exceptions;
using MedicalAppointments.Domain.ClinicalRecords;
using Microsoft.Extensions.Options;

namespace MedicalAppointments.Application.ClinicalRecords;

public sealed class ClinicalAttachmentService : IClinicalAttachmentService
{
    private readonly IClinicalRecordsAccessService _access;
    private readonly IClinicalAttachmentStore _store;
    private readonly IClinicalNoteStore _noteStore;
    private readonly IAppointmentStore _appointmentStore;
    private readonly IFileStorage _storage;
    private readonly IFileScanner _scanner;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClinicalRecordsEventNotifier _notifier;
    private readonly IAuditWriter _auditWriter;
    private readonly IRequestMetadataProvider _requestMetadata;
    private readonly ICorrelationIdProvider _correlationIdProvider;
    private readonly TimeProvider _timeProvider;
    private readonly AttachmentsOptions _options;

    public ClinicalAttachmentService(
        IClinicalRecordsAccessService access,
        IClinicalAttachmentStore store,
        IClinicalNoteStore noteStore,
        IAppointmentStore appointmentStore,
        IFileStorage storage,
        IFileScanner scanner,
        IUnitOfWork unitOfWork,
        IClinicalRecordsEventNotifier notifier,
        IAuditWriter auditWriter,
        IRequestMetadataProvider requestMetadata,
        ICorrelationIdProvider correlationIdProvider,
        TimeProvider timeProvider,
        IOptions<AttachmentsOptions> optionsAccessor)
    {
        _access = access;
        _store = store;
        _noteStore = noteStore;
        _appointmentStore = appointmentStore;
        _storage = storage;
        _scanner = scanner;
        _unitOfWork = unitOfWork;
        _notifier = notifier;
        _auditWriter = auditWriter;
        _requestMetadata = requestMetadata;
        _correlationIdProvider = correlationIdProvider;
        _timeProvider = timeProvider;
        _options = optionsAccessor.Value;
    }

    public async Task<IReadOnlyList<ClinicalAttachmentDto>> ListByNoteAsync(
        Guid noteId,
        CancellationToken cancellationToken = default)
    {
        var subject = await _access.GetSubjectAsync(cancellationToken);
        var (note, appointment) = await LoadNoteAndAppointmentAsync(noteId, cancellationToken);

        if (!_access.CanReadNotesForAppointment(appointment, subject))
        {
            throw new ForbiddenException("You do not have access to this appointment's clinical records.");
        }

        var attachments = await _store.GetByNoteAsync(noteId, cancellationToken);
        return attachments.Select(Map).ToList();
    }

    public async Task<ClinicalAttachmentDto> UploadAsync(
        UploadClinicalAttachmentCommand command,
        CancellationToken cancellationToken = default)
    {
        var now = _timeProvider.GetUtcNow();
        var subject = await _access.GetSubjectAsync(cancellationToken);

        var (note, appointment) = await LoadNoteAndAppointmentAsync(command.NoteId, cancellationToken);

        // Subir adjunto == editar el borrador: reuso CanUpdateNote (Fase 8). Solo borradores.
        if (note.IsFinal)
        {
            throw new ConflictException("Attachments can only be added to a draft note. Finalized notes are immutable.");
        }

        if (!_access.CanUpdateNote(note, subject))
        {
            throw new ForbiddenException("You do not have permission to attach files to this note.");
        }

        if (command.SizeBytes <= 0 || command.SizeBytes > _options.MaxBytes)
        {
            throw new ConflictException($"File exceeds the maximum size of {_options.MaxBytes} bytes.");
        }

        if (!IsContentTypeAllowed(command.ContentType))
        {
            throw new ConflictException($"Content type '{command.ContentType}' is not allowed.");
        }

        if (!subject.UserId.HasValue)
        {
            throw new ConflictException("An authenticated user is required to upload an attachment.");
        }

        // Hash + storage key (GUID + ext sanitizada) ANTES de tocar disco, para rollback limpio.
        var sha256Hex = await ComputeSha256Async(command.Content, cancellationToken);
        var storageKey = BuildStorageKey(note.Id, command.FileName);

        await _storage.SaveAsync(command.Content, storageKey, command.SizeBytes, cancellationToken);

        ClinicalAttachmentScanStatus scanStatus;
        DateTimeOffset? scannedAt = null;
        var scannerStage = "open";

        try
        {
            await using var readStream = await _storage.OpenReadAsync(storageKey, cancellationToken);
            scannerStage = "scan";
            var scan = await _scanner.ScanAsync(readStream, command.FileName, cancellationToken);
            scanStatus = scan.Status;
            scannedAt = now;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            await SafeDeleteAsync(storageKey, CancellationToken.None);
            throw;
        }
        catch (Exception exception)
        {
            await SafeDeleteAsync(storageKey, CancellationToken.None);
            EnqueueAudit(subject.UserId, "ClinicalRecords.AttachmentRejectedUnscanned", note.Id, now,
                new { extension = Path.GetExtension(command.FileName), sizeBytes = command.SizeBytes, sha256 = sha256Hex, failureStage = scannerStage, failureType = exception.GetType().Name });
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            throw new AttachmentScannerUnavailableException(
                "The attachment was rejected because the configured scanner could not verify it.", exception);
        }

        if (_options.Scanner.Enabled && scanStatus == ClinicalAttachmentScanStatus.Skipped)
        {
            await SafeDeleteAsync(storageKey, CancellationToken.None);
            EnqueueAudit(subject.UserId, "ClinicalRecords.AttachmentRejectedUnscanned", note.Id, now,
                new { extension = Path.GetExtension(command.FileName), sizeBytes = command.SizeBytes, sha256 = sha256Hex });
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            throw new AttachmentScannerUnavailableException(
                "The attachment was rejected because the configured scanner skipped verification.");
        }

        if (scanStatus == ClinicalAttachmentScanStatus.Infected)
        {
            // Infectado: rollback del temp, NO persistimos metadata, 409 + auditoría.
            await SafeDeleteAsync(storageKey, CancellationToken.None);
            EnqueueAudit(subject.UserId, "ClinicalRecords.AttachmentRejectedInfected", note.Id, now,
                new { extension = Path.GetExtension(command.FileName), sizeBytes = command.SizeBytes, sha256 = sha256Hex, reason = "MalwareDetected" });
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            throw new ConflictException("The file failed the virus scan and was rejected.");
        }

        var attachment = ClinicalAttachment.Create(
            note.Id,
            command.FileName,
            command.ContentType,
            command.SizeBytes,
            storageKey,
            sha256Hex,
            scanStatus,
            scannedAt,
            subject.UserId.Value,
            now);

        await _store.AddAsync(attachment, cancellationToken);

        _notifier.EnqueueAttachmentEvent(attachment, appointment, "Uploaded", now, _correlationIdProvider.CorrelationId);
        EnqueueAudit(subject.UserId, "ClinicalRecords.AttachmentUploaded", attachment.Id, now,
            new { noteId = note.Id, fileName = command.FileName, scanStatus = scanStatus.ToString() });

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Map(attachment);
    }

    public async Task<(Stream Content, string FileName, string ContentType)> DownloadAsync(
        Guid attachmentId,
        CancellationToken cancellationToken = default)
    {
        var subject = await _access.GetSubjectAsync(cancellationToken);

        var attachment = await _store.GetTrackedByIdAsync(attachmentId, cancellationToken)
            ?? throw new NotFoundException("Attachment not found.");

        var note = await _noteStore.GetTrackedByIdAsync(attachment.NoteId, cancellationToken)
            ?? throw new NotFoundException("Clinical note not found.");

        var appointment = await _appointmentStore.GetTrackedByIdAsync(note.AppointmentId, cancellationToken)
            ?? throw new NotFoundException("Appointment not found.");

        if (!_access.CanReadNotesForAppointment(appointment, subject))
        {
            throw new ForbiddenException("You do not have access to this attachment.");
        }

        if (attachment.ScanStatus == ClinicalAttachmentScanStatus.Infected)
        {
            // Defensivo: nunca debería persistir un infectado, pero por si acaso.
            throw new ConflictException("This attachment is quarantined and cannot be downloaded.");
        }

        var stream = await _storage.OpenReadAsync(attachment.StorageKey, cancellationToken);
        return (stream, attachment.FileName, attachment.ContentType);
    }

    public async Task DeleteAsync(Guid attachmentId, CancellationToken cancellationToken = default)
    {
        var now = _timeProvider.GetUtcNow();
        var subject = await _access.GetSubjectAsync(cancellationToken);

        var attachment = await _store.GetTrackedByIdAsync(attachmentId, cancellationToken)
            ?? throw new NotFoundException("Attachment not found.");

        var note = await _noteStore.GetTrackedByIdAsync(attachment.NoteId, cancellationToken)
            ?? throw new NotFoundException("Clinical note not found.");

        var appointment = await _appointmentStore.GetTrackedByIdAsync(note.AppointmentId, cancellationToken)
            ?? throw new NotFoundException("Appointment not found.");

        // Quitar adjunto == editar el borrador: reuso CanUpdateNote (8b). Solo borradores.
        if (note.IsFinal)
        {
            throw new ConflictException("Attachments on a finalized note are sealed and cannot be removed.");
        }

        if (!_access.CanUpdateNote(note, subject))
        {
            throw new ForbiddenException("You do not have permission to remove this attachment.");
        }

        if (!subject.UserId.HasValue)
        {
            throw new ConflictException("An authenticated user is required to remove an attachment.");
        }

        // Idempotente: si ya está borrado, SoftDelete es no-op y NO re-audito ni re-publica.
        if (attachment.IsDeleted)
        {
            return;
        }

        attachment.SoftDelete(subject.UserId.Value, now);

        // Auditoría SANITIZADA: loggeo extensión + size + hash, NUNCA el nombre original (filtra contexto).
        var ext = Path.GetExtension(attachment.FileName);
        EnqueueAudit(subject.UserId, "ClinicalRecords.AttachmentDeleted", attachment.Id, now,
            new { noteId = note.Id, extension = ext, sizeBytes = attachment.SizeBytes, sha256 = attachment.Sha256Hex });

        // Evento realtime metadata-only, SIN fileName (el cliente lo re-obtiene vía listByNote autorizado).
        _notifier.EnqueueAttachmentEvent(attachment, appointment, "Deleted", now, _correlationIdProvider.CorrelationId);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // NOTA: el archivo FÍSICO NO se borra aquí (trazabilidad + posible recuperación/auditoría).
        // La limpieza física de soft-deleted es deuda de background cleanup (ver Technical Debt 8c).
    }

    private async Task<(ClinicalNote Note, Domain.Appointments.Appointment Appointment)> LoadNoteAndAppointmentAsync(
        Guid noteId,
        CancellationToken cancellationToken)
    {
        var note = await _noteStore.GetTrackedByIdAsync(noteId, cancellationToken)
            ?? throw new NotFoundException("Clinical note not found.");

        var appointment = await _appointmentStore.GetTrackedByIdAsync(note.AppointmentId, cancellationToken)
            ?? throw new NotFoundException("Appointment not found.");

        return (note, appointment);
    }

    private bool IsContentTypeAllowed(string contentType)
    {
        var normalized = contentType.Trim().ToLowerInvariant();
        return _options.AllowedContentTypes.Any(x => string.Equals(x, normalized, StringComparison.OrdinalIgnoreCase));
    }

    private static string BuildStorageKey(Guid noteId, string originalFileName)
    {
        var ext = Path.GetExtension(originalFileName);
        if (string.IsNullOrWhiteSpace(ext) || ext.Length > 12 || !ext.All(c => char.IsLetterOrDigit(c) || c == '.'))
        {
            ext = ".bin";
        }

        // GUID-based key: evita traversal y colisiones. El nombre legible vive solo en metadata.
        return $"notes/{noteId:N}/{Guid.CreateVersion7():N}{ext.ToLowerInvariant()}";
    }

    private static async Task<string> ComputeSha256Async(Stream content, CancellationToken cancellationToken)
    {
        content.Position = 0;
        using var sha = SHA256.Create();
        var hash = await sha.ComputeHashAsync(content, cancellationToken);
        content.Position = 0;
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private async Task SafeDeleteAsync(string storageKey, CancellationToken cancellationToken)
    {
        try
        {
            await _storage.DeleteAsync(storageKey, cancellationToken);
        }
        catch
        {
            // Best-effort cleanup; el 409 se lanza igual.
        }
    }

    private void EnqueueAudit(Guid? userId, string action, Guid entityId, DateTimeOffset now, object? newValues)
    {
        _auditWriter.Enqueue(new AuditEntry(
            userId, action, nameof(ClinicalAttachment), entityId, now,
            _requestMetadata.IpAddress, _requestMetadata.UserAgent, null,
            newValues is null ? null : System.Text.Json.JsonSerializer.Serialize(newValues, JsonDefaults.Options),
            _correlationIdProvider.CorrelationId));
    }

    private static ClinicalAttachmentDto Map(ClinicalAttachment a) => new(
        a.Id, a.NoteId, a.FileName, a.ContentType, a.SizeBytes,
        a.ScanStatus, a.ScannedAt, a.UploadedByUserId, a.CreatedAt);
}
