namespace MemoryWiki.Contracts.Messages;

/// <summary>
/// Message published to RabbitMQ to request memory generation for a transcript.
/// Loosely follows the CloudEvents shape so downstream systems can subscribe.
/// </summary>
public sealed record GenerateMemoryMessage
{
    public string SpecVersion { get; init; } = "1.0";
    public string Type { get; init; } = "com.memorywiki.transcript.submitted";
    public string Source { get; init; } = "memorywiki/api";
    public Guid Id { get; init; } = Guid.NewGuid();
    public DateTimeOffset Time { get; init; } = DateTimeOffset.UtcNow;

    public required Guid TranscriptId { get; init; }
    public required string ObjectKey { get; init; }
    public required string IdempotencyKey { get; init; }
    public string? TenantId { get; init; }
}

public sealed record MemoryCompletedMessage
{
    public string Type { get; init; } = "com.memorywiki.memory.generated";
    public required Guid TranscriptId { get; init; }
    public required int FilesWritten { get; init; }
    public DateTimeOffset Time { get; init; } = DateTimeOffset.UtcNow;
}
