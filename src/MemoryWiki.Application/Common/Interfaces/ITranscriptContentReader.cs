namespace MemoryWiki.Application.Common.Interfaces;

/// <summary>Reads the raw transcript text back from object storage (used by the worker).</summary>
public interface ITranscriptContentReader
{
    Task<string?> ReadAsync(string objectKey, CancellationToken ct = default);
}
