using FluentAssertions;
using MemoryWiki.Domain.Enums;
using MemoryWiki.Domain.ValueObjects;
using Xunit;

namespace MemoryWiki.Application.Tests;

public class MemoryPathTests
{
    [Theory]
    [InlineData("people", "/people")]
    [InlineData("/people/", "/people")]
    [InlineData("people\\john.md", "/people/john.md")]
    [InlineData("", "/")]
    public void Normalize_produces_canonical_paths(string input, string expected) =>
        MemoryPath.Normalize(input).Value.Should().Be(expected);

    [Theory]
    [InlineData("/people/../secrets")]
    [InlineData("../etc/passwd")]
    [InlineData("/people/./x")]
    public void Normalize_rejects_traversal(string input)
    {
        var act = () => MemoryPath.Normalize(input);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void ForDocument_builds_typed_path()
    {
        MemoryPath.ForDocument(MemoryType.Person, "john-doe").Value.Should().Be("/people/john-doe.md");
        MemoryPath.ForDocument(MemoryType.Project, "memory-wiki").ToObjectKey().Should().Be("projects/memory-wiki.md");
    }

    [Fact]
    public void IsDirectory_distinguishes_files_from_dirs()
    {
        MemoryPath.Normalize("/people").IsDirectory.Should().BeTrue();
        MemoryPath.Normalize("/people/john.md").IsDirectory.Should().BeFalse();
    }
}
