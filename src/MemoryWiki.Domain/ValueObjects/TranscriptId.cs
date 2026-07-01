namespace MemoryWiki.Domain.ValueObjects;

/// <summary>Strongly-typed transcript identifier.</summary>
public readonly record struct TranscriptId(Guid Value)
{
    public static TranscriptId New() => new(Guid.NewGuid());
    public static TranscriptId From(Guid value) => new(value);
    public override string ToString() => Value.ToString();
}
