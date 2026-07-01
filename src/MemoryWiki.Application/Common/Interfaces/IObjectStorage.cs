using MemoryWiki.Domain.Entities;

namespace MemoryWiki.Application.Common.Interfaces;

public sealed record StoredObject(string Key, string Content, long SizeBytes, DateTimeOffset? LastModifiedUtc);

/// <summary>
/// Abstraction over an S3-compatible object store (MinIO / AWS S3 / Azure Blob).
/// Models the memory tree as a virtual filesystem keyed by "/"-delimited paths.
/// </summary>
public interface IObjectStorage
{
    Task EnsureBucketAsync(CancellationToken ct = default);

    Task UploadTextAsync(string key, string content, string contentType = "text/markdown", CancellationToken ct = default);

    Task<StoredObject?> DownloadTextAsync(string key, CancellationToken ct = default);

    Task<bool> ExistsAsync(string key, CancellationToken ct = default);

    /// <summary>Lists immediate children of a prefix as filesystem nodes (directories + files).</summary>
    Task<IReadOnlyList<MemoryNode>> ListAsync(string prefix, CancellationToken ct = default);

    /// <summary>Returns every object key under a prefix (recursive) — used by grep.</summary>
    Task<IReadOnlyList<string>> ListAllKeysAsync(string prefix, CancellationToken ct = default);
}
