using MedicalAppointments.Domain.Common;

namespace MedicalAppointments.Domain.ClinicalRecords;

public sealed class ClinicalAttachment : AggregateRoot
{
    private ClinicalAttachment()
    {
    }

    public Guid NoteId { get; private set; }

    public string FileName { get; private set; } = null!;

    public string ContentType { get; private set; } = null!;

    public long SizeBytes { get; private set; }

    public string StorageKey { get; private set; } = null!;

    public string Sha256Hex { get; private set; } = null!;

    public ClinicalAttachmentScanStatus ScanStatus { get; private set; }

    public DateTimeOffset? ScannedAt { get; private set; }

    public Guid UploadedByUserId { get; private set; }

    public ClinicalNote Note { get; private set; } = null!;

    public bool IsDeleted { get; private set; }
    public DateTimeOffset? DeletedAt { get; private set; }
    public Guid? DeletedByUserId { get; private set; }

    public static ClinicalAttachment Create(
        Guid noteId,
        string fileName,
        string contentType,
        long sizeBytes,
        string storageKey,
        string sha256Hex,
        ClinicalAttachmentScanStatus scanStatus,
        DateTimeOffset? scannedAt,
        Guid uploadedByUserId,
        DateTimeOffset now)
    {
        if (noteId == Guid.Empty)
        {
            throw new DomainException("Note is required.");
        }

        if (string.IsNullOrWhiteSpace(fileName))
        {
            throw new DomainException("File name is required.");
        }

        if (string.IsNullOrWhiteSpace(contentType))
        {
            throw new DomainException("Content type is required.");
        }

        if (sizeBytes <= 0)
        {
            throw new DomainException("File size must be positive.");
        }

        if (string.IsNullOrWhiteSpace(storageKey))
        {
            throw new DomainException("Storage key is required.");
        }

        if (string.IsNullOrWhiteSpace(sha256Hex) || sha256Hex.Length != 64)
        {
            throw new DomainException("SHA-256 hash must be 64 hex characters.");
        }

        if (uploadedByUserId == Guid.Empty)
        {
            throw new DomainException("Uploading user is required.");
        }

        return new ClinicalAttachment
        {
            Id = Guid.CreateVersion7(),
            NoteId = noteId,
            FileName = fileName.Trim(),
            ContentType = contentType.Trim(),
            SizeBytes = sizeBytes,
            StorageKey = storageKey,
            Sha256Hex = sha256Hex,
            ScanStatus = scanStatus,
            ScannedAt = scannedAt,
            UploadedByUserId = uploadedByUserId,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public void SoftDelete(Guid deletedByUserId, DateTimeOffset now)
    {
        if (IsDeleted)
        {
            return; // ya borrado: no-op idempotente
        }

        if (deletedByUserId == Guid.Empty)
        {
            throw new DomainException("Deleting user is required.");
        }

        IsDeleted = true;
        DeletedAt = now;
        DeletedByUserId = deletedByUserId;
        Touch(now);
    }
}