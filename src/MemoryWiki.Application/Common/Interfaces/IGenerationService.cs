using MemoryWiki.Domain.Enums;

namespace MemoryWiki.Application.Common.Interfaces;

/// <summary>One memory entity extracted from a transcript by the LLM (pass 1).</summary>
public sealed record ExtractedMemory
{
    public required MemoryType Type { get; init; }
    public required string Name { get; init; }
    public string Summary { get; init; } = string.Empty;
    public IReadOnlyList<string> Facts { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> Participants { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> Timeline { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> Decisions { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> OpenQuestions { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> RelatedTopics { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> Tags { get; init; } = Array.Empty<string>();
}

/// <summary>
/// LLM abstraction. Implementations: OpenAI (HTTP), and a Deterministic
/// offline implementation so the whole pipeline runs without an API key.
/// Two-pass design enables clean merge/update of existing memory files.
/// </summary>
public interface IGenerationService
{
    /// <summary>Pass 1 — extract structured entities + facts from a raw transcript.</summary>
    Task<IReadOnlyList<ExtractedMemory>> ExtractAsync(string transcriptTitle, string transcript, CancellationToken ct = default);

    /// <summary>
    /// Pass 2 — compose/merge a single markdown memory file. When existingMarkdown is
    /// provided, the new information is merged into it (update); otherwise a new file is authored.
    /// </summary>
    Task<string> ComposeMarkdownAsync(string? existingMarkdown, ExtractedMemory memory, string transcriptTitle, CancellationToken ct = default);
}
