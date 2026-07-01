namespace MemoryWiki.Shared;

/// <summary>Well-known constants for the memory file tree and messaging topology.</summary>
public static class MemoryTree
{
    public static readonly string[] RootDirectories = { "people", "projects", "topics", "events" };
}

public static class Messaging
{
    public const string Exchange = "memorywiki";
    public const string GenerateQueue = "memory.generate";
    public const string GenerateRoutingKey = "memory.generate";
    public const string DeadLetterExchange = "memorywiki.dlx";
    public const string DeadLetterQueue = "memory.generate.dlq";
    public const string CompletedRoutingKey = "memory.completed";
}
