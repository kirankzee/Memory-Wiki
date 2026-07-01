using System.Text;
using System.Text.RegularExpressions;

namespace MemoryWiki.Domain.ValueObjects;

/// <summary>Base for human-readable names that also expose a URL/file-safe slug.</summary>
public abstract record EntityName
{
    public string Value { get; }
    public string Slug { get; }

    protected EntityName(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Name cannot be empty.", nameof(value));
        Value = value.Trim();
        Slug = Slugify(Value);
    }

    public static string Slugify(string input)
    {
        var lowered = input.Trim().ToLowerInvariant();
        var sb = new StringBuilder(lowered.Length);
        foreach (var ch in lowered)
        {
            if (char.IsLetterOrDigit(ch)) sb.Append(ch);
            else if (ch is ' ' or '-' or '_' or '.') sb.Append('-');
        }
        var slug = Regex.Replace(sb.ToString(), "-{2,}", "-").Trim('-');
        return string.IsNullOrEmpty(slug) ? "untitled" : slug;
    }

    public override string ToString() => Value;
}

public sealed record PersonName : EntityName
{
    public PersonName(string value) : base(value) { }
}

public sealed record ProjectName : EntityName
{
    public ProjectName(string value) : base(value) { }
}
