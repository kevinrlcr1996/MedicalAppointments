using MedicalAppointments.Domain.Common;

namespace MedicalAppointments.Domain.ClinicalRecords;

/// <summary>
/// Documento DERIVADO de una receta EMITIDA (9.2). Inmutable, create-only.
/// Referencia la Prescription (D1); NO referencia Patient directamente; NO pasa por AV scan (D3).
/// Los bytes se almacenan via IFileStorage (seam 8b) PERO este aggregate solo guarda METADATOS;
/// quien genera/almacena es ClinicalDocumentService (GAP G-D/G-E, NO entregado aqui).
/// </summary>
public class ClinicalDocument : AggregateRoot
{
    public Guid PrescriptionId { get; private set; }
    public string FileName { get; private set; } = null!;      // legible, metadata only (NUNCA viaja en SignalR, D7)
    public string ContentType { get; private set; } = null!;   // p.ej. application/pdf (lo pone el service gap)
    public long SizeBytes { get; private set; }
    public string StorageKey { get; private set; } = null!;    // GUID-based sanitizado (lo pone el service via IFileStorage, gap)
    public string Sha256Hex { get; private set; } = null!;     // 64 hex (lo calcula el service gap)
    public Guid GeneratedByUserId { get; private set; }
    public DateTimeOffset GeneratedAt { get; private set; }

    // navegacion DENTRO del entity (rule 9/6): el documento sabe la receta; la receta NO navega a documentos.
    public Prescription Prescription { get; private set; } = null!;

    private ClinicalDocument() { } // EF

    public static ClinicalDocument Create(
        Guid prescriptionId,
        string fileName,
        string contentType,
        long sizeBytes,
        string storageKey,
        string sha256Hex,
        Guid generatedByUserId,
        DateTimeOffset now)
    {
        if (prescriptionId == Guid.Empty) throw new DomainException("PrescriptionId is required.");
        if (string.IsNullOrWhiteSpace(fileName)) throw new DomainException("FileName is required.");
        if (string.IsNullOrWhiteSpace(contentType)) throw new DomainException("ContentType is required.");
        if (sizeBytes <= 0) throw new DomainException("SizeBytes must be greater than zero.");
        if (string.IsNullOrWhiteSpace(storageKey)) throw new DomainException("StorageKey is required.");
        if (!IsSha256Hex(sha256Hex)) throw new DomainException("Sha256Hex must be exactly 64 hexadecimal characters.");
        if (generatedByUserId == Guid.Empty) throw new DomainException("GeneratedByUserId is required.");

        var doc = new ClinicalDocument
        {
            PrescriptionId = prescriptionId,
            FileName = fileName.Trim(),
            ContentType = contentType.Trim(),
            SizeBytes = sizeBytes,
            StorageKey = storageKey.Trim(),
            Sha256Hex = sha256Hex.Trim().ToLowerInvariant(),
            GeneratedByUserId = generatedByUserId,
            GeneratedAt = now,
        };
        doc.Touch(now);
        return doc;
    }

    private static bool IsSha256Hex(string? value)
    {
        if (value is null || value.Length != 64) return false;
        foreach (var c in value)
        {
            var ok = (c >= '0' && c <= '9') || (c >= 'a' && c <= 'f') || (c >= 'A' && c <= 'F');
            if (!ok) return false;
        }
        return true;
    }
}
