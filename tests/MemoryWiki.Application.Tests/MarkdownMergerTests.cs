using FluentAssertions;
using MemoryWiki.Application.Common.Interfaces;
using MemoryWiki.Domain.Enums;
using MemoryWiki.Infrastructure.Llm;
using Xunit;

namespace MemoryWiki.Application.Tests;

public class MarkdownMergerTests
{
    private static ExtractedMemory Person(string name, params string[] facts) => new()
    {
        Type = MemoryType.Person,
        Name = name,
        Summary = $"{name} is a participant.",
        Facts = facts
    };

    [Fact]
    public void Compose_renders_required_person_sections()
    {
        var md = MarkdownMerger.Compose(null, Person("John Doe", "Owns the API."), "Kickoff");

        md.Should().StartWith("# John Doe");
        md.Should().Contain("## Summary");
        md.Should().Contain("## Responsibilities");
        md.Should().Contain("- Owns the API.");
        md.Should().Contain("<!-- sources: Kickoff -->");
    }

    [Fact]
    public void Compose_merges_new_facts_without_losing_existing_ones()
    {
        var first = MarkdownMerger.Compose(null, Person("John", "Owns the API."), "Kickoff");
        var merged = MarkdownMerger.Compose(first, Person("John", "Leads hiring."), "Planning");

        merged.Should().Contain("- Owns the API.");   // preserved
        merged.Should().Contain("- Leads hiring.");    // added
        merged.Should().Contain("Kickoff");
        merged.Should().Contain("Planning");           // provenance accumulates
    }

    [Fact]
    public void Compose_deduplicates_repeated_facts()
    {
        var first = MarkdownMerger.Compose(null, Person("John", "Owns the API."), "Kickoff");
        var merged = MarkdownMerger.Compose(first, Person("John", "Owns the API."), "Kickoff");

        var occurrences = merged.Split("- Owns the API.").Length - 1;
        occurrences.Should().Be(1);
    }
}
