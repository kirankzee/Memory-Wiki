using FluentAssertions;
using MemoryWiki.Domain.Enums;
using MemoryWiki.Infrastructure.Llm;
using Xunit;

namespace MemoryWiki.Application.Tests;

public class DeterministicGenerationServiceTests
{
    private const string Transcript =
        """
        Alice: We agreed to migrate to RabbitMQ by 2026-02-01.
        Bob: What about the existing SQS queues?
        Alice: We will deprecate them next week.
        """;

    [Fact]
    public async Task Extract_finds_topic_and_person_memories()
    {
        var svc = new DeterministicGenerationService();
        var memories = await svc.ExtractAsync("Queue Migration", Transcript);

        memories.Should().Contain(m => m.Type == MemoryType.Person && m.Name == "Alice");
        memories.Should().Contain(m => m.Type == MemoryType.Person && m.Name == "Bob");
        memories.Should().Contain(m => m.Type == MemoryType.Topic || m.Type == MemoryType.Project);
    }

    [Fact]
    public async Task Extract_classifies_decisions_questions_and_timeline()
    {
        var svc = new DeterministicGenerationService();
        var topic = (await svc.ExtractAsync("Queue Migration", Transcript))
            .First(m => m.Type is MemoryType.Topic or MemoryType.Project);

        topic.Decisions.Should().Contain(d => d.Contains("migrate", StringComparison.OrdinalIgnoreCase));
        topic.OpenQuestions.Should().Contain(q => q.EndsWith('?'));
        topic.Timeline.Should().Contain(t => t.Contains("2026-02-01"));
    }

    [Fact]
    public async Task Extract_is_deterministic()
    {
        var svc = new DeterministicGenerationService();
        var a = await svc.ExtractAsync("T", Transcript);
        var b = await svc.ExtractAsync("T", Transcript);
        a.Select(m => m.Name).Should().Equal(b.Select(m => m.Name));
    }
}
