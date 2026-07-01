using System.Text;
using MemoryWiki.Application.Common.Interfaces;

namespace MemoryWiki.Infrastructure.Llm;

/// <summary>
/// Builds deterministic system/user prompts for the two-pass memory pipeline.
/// Pass 1 extracts structured JSON; pass 2 composes/merges a single markdown file.
/// </summary>
public sealed class PromptBuilder : IPromptBuilder
{
    private const string ExtractSystem =
        """
        You organize conversation transcripts into a persistent memory wiki.
        Extract durable, factual memories and classify each into exactly one type:
        Person, Project, Topic, or Event.
        Output ONLY a JSON object, no prose, no markdown fences.
        Be deterministic: identical input must yield identical output.
        Schema:
        {
          "memories": [
            {
              "type": "Person|Project|Topic|Event",
              "name": "canonical display name",
              "summary": "1-3 sentence factual summary",
              "facts": ["durable fact", "..."],
              "participants": ["name", "..."],
              "timeline": ["YYYY-MM-DD or relative — event", "..."],
              "decisions": ["decision made", "..."],
              "openQuestions": ["unresolved question", "..."],
              "relatedTopics": ["topic", "..."],
              "tags": ["lowercase-tag", "..."]
            }
          ]
        }
        Rules: prefer canonical names; deduplicate; omit speculation; keep facts atomic.
        """;

    private const string ComposeSystem =
        """
        You maintain markdown files in a persistent memory wiki.
        Produce a SINGLE markdown file only. No explanations, no code fences.
        Merge new information into the existing file when provided: keep prior facts,
        add new ones, deduplicate, and never delete still-valid information.
        Keep the exact section headings given. Use deterministic ordering (stable, alphabetical
        within a section where order is not meaningful).
        End the file with an HTML comment containing provenance, e.g. <!-- sources: ... -->.
        """;

    public Prompt BuildExtractPrompt(string transcriptTitle, string transcript)
    {
        var user = new StringBuilder()
            .AppendLine($"Transcript title: {transcriptTitle}")
            .AppendLine("Transcript:")
            .AppendLine("\"\"\"")
            .AppendLine(transcript)
            .AppendLine("\"\"\"")
            .ToString();
        return new Prompt(ExtractSystem, user);
    }

    public Prompt BuildComposePrompt(string? existingMarkdown, ExtractedMemory memory, string transcriptTitle)
    {
        var headings = HeadingsFor(memory);
        var sb = new StringBuilder()
            .AppendLine($"Entity type: {memory.Type}")
            .AppendLine($"Entity name: {memory.Name}")
            .AppendLine($"New information sourced from transcript: \"{transcriptTitle}\"")
            .AppendLine()
            .AppendLine("Required section headings (in order):")
            .AppendLine(string.Join(", ", headings))
            .AppendLine()
            .AppendLine("Structured new facts (JSON):")
            .AppendLine(SerializeMemory(memory));

        if (!string.IsNullOrWhiteSpace(existingMarkdown))
        {
            sb.AppendLine().AppendLine("EXISTING FILE (merge into this, do not lose facts):")
              .AppendLine("\"\"\"").AppendLine(existingMarkdown).AppendLine("\"\"\"");
        }

        return new Prompt(ComposeSystem, sb.ToString());
    }

    public static string[] HeadingsFor(ExtractedMemory m) => m.Type switch
    {
        Domain.Enums.MemoryType.Person =>
            new[] { "Summary", "Responsibilities", "Meetings", "Projects", "Important Decisions", "Relationships", "Open Questions" },
        _ =>
            new[] { "Summary", "Participants", "Timeline", "Important Facts", "Decisions", "Open Questions", "Related Topics" }
    };

    private static string SerializeMemory(ExtractedMemory m) =>
        System.Text.Json.JsonSerializer.Serialize(m, new System.Text.Json.JsonSerializerOptions
        {
            WriteIndented = true
        });
}
