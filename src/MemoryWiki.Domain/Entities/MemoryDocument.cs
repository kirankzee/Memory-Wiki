using MemoryWiki.Domain.Common;
using MemoryWiki.Domain.Enums;

namespace MemoryWiki.Domain.Entities;

/// <summary>
/// Database index row for a markdown memory file stored in the object store.
/// The authoritative content lives in S3; this row powers fast listing,
/// grep candidate selection, versioning and soft-delete.
/// </summary>
public sealed class MemoryDocument : Entity
{
    public string Path { get; private set; } = string.Empty;     // "/people/john-doe.md"
    public string ObjectKey { get; private set; } = string.Empty; // "people/john-doe.md"
    public MemoryType Type { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string Slug { get; private set; } = string.Empty;
    public int Version { get; private set; }
    public string ContentHash { get; private set; } = string.Empty;
    public long SizeBytes { get; private set; }
    public string? TenantId { get; private set; }
    public bool IsDeleted { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }

    private MemoryDocument() { }

    public static MemoryDocument Create(string path, string objectKey, MemoryType type, string title, string slug,
        string contentHash, long sizeBytes, string? tenantId = null)
    {
        var now = DateTimeOffset.UtcNow;
        return new MemoryDocument
        {
            Id = Guid.NewGuid(),
            Path = path,
            ObjectKey = objectKey,
            Type = type,
            Title = title,
            Slug = slug,
            Version = 1,
            ContentHash = contentHash,
            SizeBytes = sizeBytes,
            TenantId = tenantId,
            IsDeleted = false,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };
    }

    public void ApplyUpdate(string contentHash, long sizeBytes)
    {
        if (contentHash == ContentHash) return; // no-op, content unchanged
        Version++;
        ContentHash = contentHash;
        SizeBytes = sizeBytes;
        IsDeleted = false;
        UpdatedAtUtc = DateTimeOffset.UtcNow;
    }

    public void SoftDelete()
    {
        IsDeleted = true;
        UpdatedAtUtc = DateTimeOffset.UtcNow;
    }
}

/// <summary>
/// Lightweight read model for a node (directory or file) in the memory tree.
/// Produced by the storage layer for `ls`.
/// </summary>
public sealed record MemoryNode(string Name, string Path, bool IsDirectory, long SizeBytes, DateTimeOffset? LastModifiedUtc);
