using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using MemoryWiki.Application.Common.Interfaces;
using MemoryWiki.Domain.Enums;
using MemoryWiki.Infrastructure.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MemoryWiki.Infrastructure.Llm;

/// <summary>
/// OpenAI-backed implementation using the Chat Completions HTTP API directly (no SDK churn).
/// Resilience (retry/timeout) is applied via the injected <see cref="HttpClient"/> (Polly).
/// Falls back to the deterministic merger if the model returns unusable content.
/// </summary>
public sealed class OpenAiGenerationService(
    HttpClient http,
    IPromptBuilder prompts,
    IOptions<OpenAiOptions> options,
    ILogger<OpenAiGenerationService> logger) : IGenerationService
{
    private readonly OpenAiOptions _opt = options.Value;
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public async Task<IReadOnlyList<ExtractedMemory>> ExtractAsync(string transcriptTitle, string transcript, CancellationToken ct = default)
    {
        var prompt = prompts.BuildExtractPrompt(transcriptTitle, transcript);
        var content = await ChatAsync(prompt.System, prompt.User, jsonMode: true, ct);

        try
        {
            var parsed = JsonSerializer.Deserialize<ExtractEnvelope>(content, Json);
            if (parsed?.Memories is { Count: > 0 } list)
                return list.Select(Map).ToList();
        }
        catch (JsonException ex)
        {
            logger.LogWarning(ex, "OpenAI extract returned non-JSON; returning empty extraction.");
        }
        return Array.Empty<ExtractedMemory>();
    }

    public async Task<string> ComposeMarkdownAsync(string? existingMarkdown, ExtractedMemory memory, string transcriptTitle, CancellationToken ct = default)
    {
        var prompt = prompts.BuildComposePrompt(existingMarkdown, memory, transcriptTitle);
        try
        {
            var content = await ChatAsync(prompt.System, prompt.User, jsonMode: false, ct);
            if (!string.IsNullOrWhiteSpace(content) && content.Contains('#'))
                return content.Trim() + "\n";
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "OpenAI compose failed; falling back to deterministic merge.");
        }
        return MarkdownMerger.Compose(existingMarkdown, memory, transcriptTitle);
    }

    private async Task<string> ChatAsync(string system, string user, bool jsonMode, CancellationToken ct)
    {
        var payload = new Dictionary<string, object?>
        {
            ["model"] = _opt.Model,
            ["temperature"] = _opt.Temperature,
            ["messages"] = new object[]
            {
                new { role = "system", content = system },
                new { role = "user", content = user }
            }
        };
        if (jsonMode) payload["response_format"] = new { type = "json_object" };

        using var resp = await http.PostAsJsonAsync("chat/completions", payload, Json, ct);
        resp.EnsureSuccessStatusCode();
        var result = await resp.Content.ReadFromJsonAsync<ChatResponse>(Json, ct);
        return result?.Choices?.FirstOrDefault()?.Message?.Content ?? string.Empty;
    }

    private static ExtractedMemory Map(MemoryDto d) => new()
    {
        Type = Enum.TryParse<MemoryType>(d.Type, true, out var t) ? t : MemoryType.Topic,
        Name = string.IsNullOrWhiteSpace(d.Name) ? "Untitled" : d.Name,
        Summary = d.Summary ?? string.Empty,
        Facts = d.Facts ?? new(),
        Participants = d.Participants ?? new(),
        Timeline = d.Timeline ?? new(),
        Decisions = d.Decisions ?? new(),
        OpenQuestions = d.OpenQuestions ?? new(),
        RelatedTopics = d.RelatedTopics ?? new(),
        Tags = d.Tags ?? new()
    };

    private sealed record ExtractEnvelope([property: JsonPropertyName("memories")] List<MemoryDto>? Memories);

    private sealed record MemoryDto
    {
        public string Type { get; init; } = "Topic";
        public string Name { get; init; } = "";
        public string? Summary { get; init; }
        public List<string>? Facts { get; init; }
        public List<string>? Participants { get; init; }
        public List<string>? Timeline { get; init; }
        public List<string>? Decisions { get; init; }
        public List<string>? OpenQuestions { get; init; }
        public List<string>? RelatedTopics { get; init; }
        public List<string>? Tags { get; init; }
    }

    private sealed record ChatResponse(List<Choice>? Choices);
    private sealed record Choice(Message? Message);
    private sealed record Message(string? Content);
}
