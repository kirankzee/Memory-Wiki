using MemoryWiki.Domain.Common;
using MemoryWiki.Domain.Enums;
using MemoryWiki.Domain.Events;
using MemoryWiki.Domain.ValueObjects;

namespace MemoryWiki.Domain.Entities;

/// <summary>
/// Aggregate root representing an ingested conversation transcript.
/// The raw content lives in object storage; metadata lives in PostgreSQL.
/// </summary>
public sealed class Transcript : AggregateRoot
{
    public string Title { get; private set; } = string.Empty;
    public string ObjectKey { get; private set; } = string.Empty;
    public long SizeBytes { get; private set; }
    public string ContentHash { get; private set; } = string.Empty;
    public TranscriptStatus Status { get; private set; }
    public string? FailureReason { get; private set; }
    public string? TenantId { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }

    private Transcript() { } // EF

    public static Transcript Create(string title, string objectKey, long sizeBytes, string contentHash, string? tenantId = null)
    {
        if (string.IsNullOrWhiteSpace(title)) throw new ArgumentException("Title is required.", nameof(title));

        var now = DateTimeOffset.UtcNow;
        var transcript = new Transcript
        {
            Id = Guid.NewGuid(),
            Title = title.Trim(),
            ObjectKey = objectKey,
            SizeBytes = sizeBytes,
            ContentHash = contentHash,
            Status = TranscriptStatus.Received,
            TenantId = tenantId,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };
        return transcript;
    }

    public TranscriptId TranscriptId => TranscriptId.From(Id);

    /// <summary>Assigns the object-store key once the id has been generated.</summary>
    public void AssignObjectKey(string objectKey)
    {
        if (string.IsNullOrWhiteSpace(objectKey)) throw new ArgumentException("Object key is required.", nameof(objectKey));
        ObjectKey = objectKey;
        Touch();
    }

    public void MarkQueued()
    {
        Status = TranscriptStatus.Queued;
        Touch();
        Raise(new TranscriptSubmitted(TranscriptId, ObjectKey));
    }

    public void MarkProcessing()
    {
        Status = TranscriptStatus.Processing;
        Touch();
    }

    public void MarkCompleted(int filesWritten)
    {
        Status = TranscriptStatus.Completed;
        FailureReason = null;
        Touch();
        Raise(new MemoryGenerated(TranscriptId, filesWritten));
    }

    public void MarkFailed(string reason)
    {
        Status = TranscriptStatus.Failed;
        FailureReason = reason;
        Touch();
    }

    private void Touch() => UpdatedAtUtc = DateTimeOffset.UtcNow;
}
