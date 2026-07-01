using MemoryWiki.Domain.Enums;

namespace MemoryWiki.Domain.ValueObjects;

/// <summary>
/// A safe, normalized path inside the memory tree, e.g. "/people/john-doe.md".
/// Guards against traversal ("..") and restricts roots to the known directories.
/// </summary>
public sealed record MemoryPath
{
    public static readonly IReadOnlyDictionary<MemoryType, string> Roots = new Dictionary<MemoryType, string>
    {
        [MemoryType.Person] = "people",
        [MemoryType.Project] = "projects",
        [MemoryType.Topic] = "topics",
        [MemoryType.Event] = "events"
    };

    public string Value { get; }

    private MemoryPath(string value) => Value = value;

    /// <summary>Normalizes any user-supplied path. Throws on traversal attempts.</summary>
    public static MemoryPath Normalize(string? raw)
    {
        var path = (raw ?? "/").Replace('\\', '/').Trim();
        if (!path.StartsWith('/')) path = "/" + path;

        var segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (segments.Any(s => s is ".." or "."))
            throw new ArgumentException("Path traversal is not allowed.", nameof(raw));

        return new MemoryPath("/" + string.Join('/', segments));
    }

    public static MemoryPath ForDocument(MemoryType type, string slug) =>
        new($"/{Roots[type]}/{slug}.md");

    public bool IsDirectory => !Value.EndsWith(".md", StringComparison.OrdinalIgnoreCase);

    /// <summary>Object-store key (no leading slash), e.g. "people/john-doe.md".</summary>
    public string ToObjectKey() => Value.TrimStart('/');

    public override string ToString() => Value;
}
