namespace MemoryWiki.Contracts.Dtos;

public sealed record CreateTranscriptResponse(Guid Id, string Status);

public sealed record TranscriptDto(
    Guid Id,
    string Title,
    string Status,
    long SizeBytes,
    string? FailureReason,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc);

public sealed record MemoryEntryDto(string Name, string Path, string Kind, long SizeBytes, DateTimeOffset? LastModifiedUtc);

public sealed record DirectoryListingDto(string Path, IReadOnlyList<MemoryEntryDto> Entries);

public sealed record MemoryFileDto(string Path, string Content, long SizeBytes, DateTimeOffset? LastModifiedUtc);

public sealed record GrepMatchDto(string Path, int LineNumber, string Line);

public sealed record GrepResultDto(string Query, int FileCount, int MatchCount, IReadOnlyList<GrepMatchDto> Matches);
