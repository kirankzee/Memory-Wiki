using MemoryWiki.Domain.Common;
using MemoryWiki.Domain.Enums;

namespace MemoryWiki.Domain.Entities;

/// <summary>
/// Tracks an asynchronous memory-generation job for a transcript.
/// Used for idempotency (one job per transcript) and retry accounting.
/// </summary>
public sealed class ProcessingJob : Entity
{
    public Guid TranscriptId { get; private set; }
    public JobStatus Status { get; private set; }
    public int Attempts { get; private set; }
    public string? LastError { get; private set; }
    public string? IdempotencyKey { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset? StartedAtUtc { get; private set; }
    public DateTimeOffset? CompletedAtUtc { get; private set; }

    private ProcessingJob() { }

    public static ProcessingJob Create(Guid transcriptId, string idempotencyKey) => new()
    {
        Id = Guid.NewGuid(),
        TranscriptId = transcriptId,
        Status = JobStatus.Pending,
        IdempotencyKey = idempotencyKey,
        CreatedAtUtc = DateTimeOffset.UtcNow
    };

    public void Start()
    {
        Status = JobStatus.InProgress;
        Attempts++;
        StartedAtUtc = DateTimeOffset.UtcNow;
    }

    public void Succeed()
    {
        Status = JobStatus.Succeeded;
        LastError = null;
        CompletedAtUtc = DateTimeOffset.UtcNow;
    }

    public void Fail(string error, int maxAttempts)
    {
        LastError = error;
        Status = Attempts >= maxAttempts ? JobStatus.DeadLettered : JobStatus.Failed;
        CompletedAtUtc = DateTimeOffset.UtcNow;
    }
}
