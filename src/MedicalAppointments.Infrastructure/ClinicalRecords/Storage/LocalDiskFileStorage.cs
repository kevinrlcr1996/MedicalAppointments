using MedicalAppointments.Application.ClinicalRecords.Storage;
using Microsoft.Extensions.Options;

namespace MedicalAppointments.Infrastructure.ClinicalRecords.Storage;

public sealed class LocalDiskFileStorage : IFileStorage
{
    private readonly string _root;

    public LocalDiskFileStorage(IOptions<AttachmentsOptions> optionsAccessor)
    {
        _root = Path.GetFullPath(optionsAccessor.Value.StorageRoot);
        Directory.CreateDirectory(_root);
    }

    public async Task<StoredFile> SaveAsync(Stream content, string storageKey, long expectedSize, CancellationToken cancellationToken = default)
    {
        var fullPath = ResolveSafe(storageKey);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);

        await using var fs = new FileStream(fullPath, FileMode.CreateNew, FileAccess.Write, FileShare.None);
        content.Position = 0;
        await content.CopyToAsync(fs, cancellationToken);
        content.Position = 0;

        return new StoredFile(storageKey, new FileInfo(fullPath).Length);
    }

    public Task<Stream> OpenReadAsync(string storageKey, CancellationToken cancellationToken = default)
    {
        var fullPath = ResolveSafe(storageKey);
        if (!File.Exists(fullPath))
        {
            throw new FileNotFoundException("Stored file not found.", fullPath);
        }

        Stream fs = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read);
        return Task.FromResult(fs);
    }

    public Task DeleteAsync(string storageKey, CancellationToken cancellationToken = default)
    {
        var fullPath = ResolveSafe(storageKey);
        if (File.Exists(fullPath))
        {
            File.Delete(fullPath);
        }

        return Task.CompletedTask;
    }

    private string ResolveSafe(string storageKey)
    {
        // Doble defensa contra traversal: el key ya es GUID-based, pero validamos que quede bajo _root.
        var fullPath = Path.GetFullPath(Path.Combine(_root, storageKey));
        if (!fullPath.StartsWith(_root, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Invalid storage key.");
        }

        return fullPath;
    }
}