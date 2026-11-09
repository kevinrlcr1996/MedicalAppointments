using MedicalAppointments.Domain.ClinicalRecords;

namespace MedicalAppointments.Application.ClinicalRecords.Storage;

public sealed record ScanResult(ClinicalAttachmentScanStatus Status, string? Message);

public interface IFileScanner
{
    // Recibe el stream ya guardado (o bytes) y dictamina Clean/Infected/Skipped.
    Task<ScanResult> ScanAsync(Stream content, string fileName, CancellationToken cancellationToken = default);
}