using MediatR;
using MemoryWiki.Application.Common.Interfaces;
using MemoryWiki.Contracts.Dtos;
using MemoryWiki.Domain.ValueObjects;

namespace MemoryWiki.Application.Memories.Queries;

/// <summary>REST equivalent of `ls` over the memory tree.</summary>
public sealed record ListMemoryQuery(string Path, string? TenantId) : IRequest<DirectoryListingDto>;

public sealed class ListMemoryHandler(IObjectStorage storage) : IRequestHandler<ListMemoryQuery, DirectoryListingDto>
{
    public async Task<DirectoryListingDto> Handle(ListMemoryQuery request, CancellationToken ct)
    {
        var path = MemoryPath.Normalize(request.Path);
        var prefix = path.Value == "/" ? string.Empty : path.ToObjectKey().TrimEnd('/') + "/";
        if (!string.IsNullOrWhiteSpace(request.TenantId))
            prefix = $"{request.TenantId}/{prefix}";

        var nodes = await storage.ListAsync(prefix, ct);
        var entries = nodes
            .Select(n => new MemoryEntryDto(n.Name, n.Path, n.IsDirectory ? "dir" : "file", n.SizeBytes, n.LastModifiedUtc))
            .OrderByDescending(e => e.Kind == "dir")
            .ThenBy(e => e.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return new DirectoryListingDto(path.Value, entries);
    }
}
