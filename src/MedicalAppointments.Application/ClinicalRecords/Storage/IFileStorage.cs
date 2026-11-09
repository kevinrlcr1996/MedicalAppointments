namespace MedicalAppointments.Application.ClinicalRecords.Storage;

public sealed record StoredFile(string StorageKey, long SizeBytes);

public interface IFileStorage
{
    // key ya sanitizado por el servicio (GUID + ext). Devuelve el key persistido.
    Task<StoredFile> SaveAsync(Stream content, string storageKey, long expectedSize, CancellationToken cancellationToken = default);

    Task<Stream> OpenReadAsync(string storageKey, CancellationToken cancellationToken = default);

    // Solo cleanup técnico interno (rollback de uploads fallidos/infectados). NO expuesto como endpoint.
    Task DeleteAsync(string storageKey, CancellationToken cancellationToken = default);
}