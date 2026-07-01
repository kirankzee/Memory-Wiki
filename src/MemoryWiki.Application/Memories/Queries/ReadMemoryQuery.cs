using MediatR;
using MemoryWiki.Application.Common.Interfaces;
using MemoryWiki.Contracts.Dtos;
using MemoryWiki.Domain.ValueObjects;

namespace MemoryWiki.Application.Memories.Queries;

/// <summary>REST equivalent of `cat` for a single memory file.</summary>
public sealed record ReadMemoryQuery(string Path, string? TenantId) : IRequest<MemoryFileDto?>;

public sealed class ReadMemoryHandler(IObjectStorage storage) : IRequestHandler<ReadMemoryQuery, MemoryFileDto?>
{
    public async Task<MemoryFileDto?> Handle(ReadMemoryQuery request, CancellationToken ct)
    {
        var path = MemoryPath.Normalize(request.Path);
        if (path.IsDirectory) return null;

        var key = string.IsNullOrWhiteSpace(request.TenantId)
            ? path.ToObjectKey()
            : $"{request.TenantId}/{path.ToObjectKey()}";

        var obj = await storage.DownloadTextAsync(key, ct);
        return obj is null ? null : new MemoryFileDto(path.Value, obj.Content, obj.SizeBytes, obj.LastModifiedUtc);
    }
}
