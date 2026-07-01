namespace MemoryWiki.Domain.Enums;

public enum TranscriptStatus
{
    Received = 0,
    Queued = 1,
    Processing = 2,
    Completed = 3,
    Failed = 4
}

public enum JobStatus
{
    Pending = 0,
    InProgress = 1,
    Succeeded = 2,
    Failed = 3,
    DeadLettered = 4
}

/// <summary>Top-level directory a memory document belongs to.</summary>
public enum MemoryType
{
    Person = 0,   // /people
    Project = 1,  // /projects
    Topic = 2,    // /topics
    Event = 3     // /events
}
