using System.Collections.Concurrent;
using MedicalAppointments.Application.ClinicalRecords.Storage;

namespace MedicalAppointments.IntegrationTests;

public sealed class InMemoryTestFileStorage : IFileStorage
{
    private readonly ConcurrentDictionary<string, byte[]> _files = new(StringComparer.Ordinal);

    public int FileCount => _files.Count;

    public async Task<StoredFile> SaveAsync(
        Stream content,
        string storageKey,
        long expectedSize,
        CancellationToken cancellationToken = default)
    {
        using var buffer = new MemoryStream();
        content.Position = 0;
        await content.CopyToAsync(buffer, cancellationToken);
        var bytes = buffer.ToArray();
        if (bytes.LongLength != expectedSize)
        {
            throw new IOException("Stored file size mismatch.");
        }

        if (!_files.TryAdd(storageKey, bytes))
        {
            throw new IOException("Storage key already exists.");
        }

        content.Position = 0;
        return new StoredFile(storageKey, bytes.LongLength);
    }

    public Task<Stream> OpenReadAsync(string storageKey, CancellationToken cancellationToken = default)
    {
        if (!_files.TryGetValue(storageKey, out var bytes))
        {
            throw new FileNotFoundException("Stored test file not found.");
        }

        Stream stream = new MemoryStream(bytes, writable: false);
        return Task.FromResult(stream);
    }

    public Task DeleteAsync(string storageKey, CancellationToken cancellationToken = default)
    {
        _files.TryRemove(storageKey, out _);
        return Task.CompletedTask;
    }
}
