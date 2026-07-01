using System.Text;
using MemoryWiki.Application.Common.Interfaces;
using MemoryWiki.Domain.Enums;

namespace MemoryWiki.Infrastructure.Llm;

/// <summary>
/// Deterministic markdown renderer + merger. Renders an <see cref="ExtractedMemory"/> into a
/// sectioned markdown file and, when an existing file is supplied, unions bullet lists per
/// section so new transcripts ENRICH rather than overwrite memory. Pure and unit-testable.
/// </summary>
public static class MarkdownMerger
{
    public static string Compose(string? existing, ExtractedMemory memory, string sourceTitle)
    {
        var headings = PromptBuilder.HeadingsFor(memory);
        var incoming = MapToSections(memory, sourceTitle);

        var (existingSummary, existingSections, existingSources) = Parse(existing);

        var sb = new StringBuilder();
        sb.Append("# ").Append(memory.Name.Trim()).Append("\n\n");

        // Summary: keep existing prose if present, otherwise use the new summary.
        var summary = !string.IsNullOrWhiteSpace(existingSummary) ? existingSummary! : memory.Summary;
        sb.Append("## Summary\n").Append(string.IsNullOrWhiteSpace(summary) ? "_No summary yet._" : summary.Trim()).Append("\n\n");

        foreach (var heading in headings.Where(h => h != "Summary"))
        {
            var merged = Union(
                existingSections.TryGetValue(heading, out var prev) ? prev : new List<string>(),
                incoming.TryGetValue(heading, out var next) ? next : new List<string>());

            sb.Append("## ").Append(heading).Append('\n');
            if (merged.Count == 0) sb.Append("_None recorded._\n");
            else foreach (var item in merged) sb.Append("- ").Append(item).Append('\n');
            sb.Append('\n');
        }

        var sources = existingSources;
        if (!sources.Contains(sourceTitle, StringComparer.OrdinalIgnoreCase)) sources.Add(sourceTitle);
        sb.Append("<!-- sources: ").Append(string.Join(" | ", sources)).Append(" -->\n");

        return sb.ToString();
    }

    private static Dictionary<string, List<string>> MapToSections(ExtractedMemory m, string sourceTitle)
    {
        var meetings = m.Timeline.Count > 0 ? m.Timeline.ToList() : new List<string> { sourceTitle };
        return m.Type == MemoryType.Person
            ? new()
            {
                ["Responsibilities"] = m.Facts.ToList(),
                ["Meetings"] = meetings,
                ["Projects"] = m.RelatedTopics.ToList(),
                ["Important Decisions"] = m.Decisions.ToList(),
                ["Relationships"] = m.Participants.ToList(),
                ["Open Questions"] = m.OpenQuestions.ToList()
            }
            : new()
            {
                ["Participants"] = m.Participants.ToList(),
                ["Timeline"] = m.Timeline.ToList(),
                ["Important Facts"] = m.Facts.ToList(),
                ["Decisions"] = m.Decisions.ToList(),
                ["Open Questions"] = m.OpenQuestions.ToList(),
                ["Related Topics"] = m.RelatedTopics.ToList()
            };
    }

    private static List<string> Union(List<string> a, List<string> b)
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var result = new List<string>();
        foreach (var item in a.Concat(b))
        {
            var trimmed = item.Trim();
            if (trimmed.Length == 0 || trimmed.StartsWith('_')) continue;
            if (seen.Add(trimmed)) result.Add(trimmed);
        }
        result.Sort(StringComparer.OrdinalIgnoreCase);
        return result;
    }

    /// <summary>Parses a previously-rendered file back into (summary, sections, sources).</summary>
    private static (string? Summary, Dictionary<string, List<string>> Sections, List<string> Sources) Parse(string? markdown)
    {
        var sections = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        var sources = new List<string>();
        if (string.IsNullOrWhiteSpace(markdown)) return (null, sections, sources);

        string? current = null;
        var summary = new StringBuilder();
        foreach (var raw in markdown.Split('\n'))
        {
            var line = raw.TrimEnd('\r');
            if (line.StartsWith("## "))
            {
                current = line[3..].Trim();
                if (!sections.ContainsKey(current)) sections[current] = new List<string>();
                continue;
            }
            if (line.StartsWith("<!-- sources:"))
            {
                var inner = line.Replace("<!-- sources:", "").Replace("-->", "").Trim();
                sources.AddRange(inner.Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
                continue;
            }
            if (current is null || line.StartsWith("# ")) continue;

            if (string.Equals(current, "Summary", StringComparison.OrdinalIgnoreCase))
            {
                if (line.Length > 0 && !line.StartsWith('_')) summary.AppendLine(line);
            }
            else if (line.StartsWith("- "))
            {
                sections[current].Add(line[2..].Trim());
            }
        }

        var summaryText = summary.ToString().Trim();
        return (summaryText.Length == 0 ? null : summaryText, sections, sources);
    }
}
