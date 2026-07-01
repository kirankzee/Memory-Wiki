using MemoryWiki.Domain.Common;
using MemoryWiki.Domain.ValueObjects;

namespace MemoryWiki.Domain.Events;

public sealed record TranscriptSubmitted(TranscriptId TranscriptId, string ObjectKey)
    : IDomainEvent
{
    public DateTimeOffset OccurredOnUtc { get; } = DateTimeOffset.UtcNow;
}

public sealed record MemoryGenerated(TranscriptId TranscriptId, int FilesWritten)
    : IDomainEvent
{
    public DateTimeOffset OccurredOnUtc { get; } = DateTimeOffset.UtcNow;
}
