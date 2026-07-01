using System.Text.RegularExpressions;
using MemoryWiki.Application.Common.Interfaces;
using MemoryWiki.Domain.Enums;

namespace MemoryWiki.Infrastructure.Llm;

/// <summary>
/// Offline, dependency-free implementation of <see cref="IGenerationService"/> so the entire
/// pipeline runs with a single command and zero API keys. Uses lightweight heuristics to
/// extract participants, facts, decisions, timeline and open questions from a transcript.
/// Output is fully deterministic, which makes it ideal for tests and local demos.
/// </summary>
public sealed class DeterministicGenerationService : IGenerationService
{
    private static readonly Regex SpeakerLine = new(@"^\s*([A-Z][\w .'\-]{1,40}?):\s*(.+)$", RegexOptions.Compiled);
    private static readonly Regex DateLike = new(@"\b(\d{4}-\d{2}-\d{2}|\d{1,2}/\d{1,2}/\d{2,4}|today|tomorrow|next week|monday|tuesday|wednesday|thursday|friday)\b",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex DecisionLike = new(@"\b(decide[ds]?|agreed|we will|let's|action item|going to|plan to|committed)\b",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public Task<IReadOnlyList<ExtractedMemory>> ExtractAsync(string transcriptTitle, string transcript, CancellationToken ct = default)
    {
        var lines = transcript.Replace("\r\n", "\n").Split('\n');
        var speakers = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        var allUtterances = new List<string>();

        foreach (var line in lines)
        {
            var m = SpeakerLine.Match(line);
            if (m.Success)
            {
                var name = m.Groups[1].Value.Trim();
                var text = m.Groups[2].Value.Trim();
                if (!speakers.TryGetValue(name, out var list)) speakers[name] = list = new List<string>();
                list.Add(text);
                allUtterances.Add(text);
            }
            else if (!string.IsNullOrWhiteSpace(line))
            {
                allUtterances.Add(line.Trim());
            }
        }

        var sentences = allUtterances
            .SelectMany(u => Regex.Split(u, @"(?<=[.!?])\s+"))
            .Select(s => s.Trim())
            .Where(s => s.Length > 8)
            .ToList();

        var decisions = sentences.Where(s => DecisionLike.IsMatch(s)).Distinct().Take(15).ToList();
        var timeline = sentences.Where(s => DateLike.IsMatch(s)).Distinct().Take(15).ToList();
        var questions = sentences.Where(s => s.EndsWith('?')).Distinct().Take(15).ToList();
        var facts = sentences.Where(s => !s.EndsWith('?')).Distinct().Take(25).ToList();
        var tags = TopKeywords(sentences, 8);

        var memories = new List<ExtractedMemory>();

        // One Topic/Project memory representing the conversation itself.
        var topicType = transcriptTitle.Contains("project", StringComparison.OrdinalIgnoreCase)
            ? MemoryType.Project : MemoryType.Topic;
        memories.Add(new ExtractedMemory
        {
            Type = topicType,
            Name = CleanTitle(transcriptTitle),
            Summary = $"Memory generated from the conversation \"{CleanTitle(transcriptTitle)}\" with {speakers.Count} participant(s).",
            Facts = facts,
            Participants = speakers.Keys.ToList(),
            Timeline = timeline,
            Decisions = decisions,
            OpenQuestions = questions,
            RelatedTopics = tags.Select(Capitalize).ToList(),
            Tags = tags
        });

        // One Person memory per speaker.
        foreach (var (name, utterances) in speakers)
        {
            var personFacts = utterances.SelectMany(u => Regex.Split(u, @"(?<=[.!?])\s+"))
                .Select(s => s.Trim()).Where(s => s.Length > 8).Distinct().Take(15).ToList();
            memories.Add(new ExtractedMemory
            {
                Type = MemoryType.Person,
                Name = name,
                Summary = $"{name} participated in \"{CleanTitle(transcriptTitle)}\".",
                Facts = personFacts,
                Participants = speakers.Keys.Where(k => !k.Equals(name, StringComparison.OrdinalIgnoreCase)).ToList(),
                Timeline = new List<string> { CleanTitle(transcriptTitle) },
                Decisions = personFacts.Where(s => DecisionLike.IsMatch(s)).ToList(),
                OpenQuestions = personFacts.Where(s => s.EndsWith('?')).ToList(),
                RelatedTopics = new List<string> { CleanTitle(transcriptTitle) },
                Tags = tags
            });
        }

        return Task.FromResult<IReadOnlyList<ExtractedMemory>>(memories);
    }

    public Task<string> ComposeMarkdownAsync(string? existingMarkdown, ExtractedMemory memory, string transcriptTitle, CancellationToken ct = default)
        => Task.FromResult(MarkdownMerger.Compose(existingMarkdown, memory, transcriptTitle));

    private static string CleanTitle(string title)
    {
        var t = Regex.Replace(title.Trim(), @"\.(txt|md)$", "", RegexOptions.IgnoreCase);
        return string.IsNullOrWhiteSpace(t) ? "Untitled Conversation" : t;
    }

    private static string Capitalize(string s) => s.Length == 0 ? s : char.ToUpperInvariant(s[0]) + s[1..];

    private static List<string> TopKeywords(IEnumerable<string> sentences, int k)
    {
        var stop = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        { "the","and","that","this","with","have","will","from","they","what","when","your","about","there","their","would","could","should","being","been" };
        var counts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var word in sentences.SelectMany(s => Regex.Matches(s.ToLowerInvariant(), @"[a-z]{4,}").Select(m => m.Value)))
        {
            if (stop.Contains(word)) continue;
            counts[word] = counts.GetValueOrDefault(word) + 1;
        }
        return counts.OrderByDescending(p => p.Value).ThenBy(p => p.Key).Take(k).Select(p => p.Key).ToList();
    }
}
