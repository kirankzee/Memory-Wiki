using System.Text.RegularExpressions;
using MediatR;
using MemoryWiki.Application.Common.Interfaces;
using MemoryWiki.Contracts.Dtos;
using MemoryWiki.Shared;

namespace MemoryWiki.Application.Memories.Queries;

/// <summary>REST equivalent of `grep` across every markdown memory file.</summary>
public sealed record SearchMemoryQuery(string Query, string? TenantId, bool IgnoreCase = true, int MaxResults = 200)
    : IRequest<GrepResultDto>;

public sealed class SearchMemoryHandler(IObjectStorage storage) : IRequestHandler<SearchMemoryQuery, GrepResultDto>
{
    public async Task<GrepResultDto> Handle(SearchMemoryQuery request, CancellationToken ct)
    {
        var options = RegexOptions.Compiled | (request.IgnoreCase ? RegexOptions.IgnoreCase : RegexOptions.None);
        Regex regex;
        try { regex = new Regex(request.Query, options); }
        catch (ArgumentException) { regex = new Regex(Regex.Escape(request.Query), options); }

        var matches = new List<GrepMatchDto>();
        var fileCount = 0;

        foreach (var root in MemoryTree.RootDirectories)
        {
            var prefix = string.IsNullOrWhiteSpace(request.TenantId) ? $"{root}/" : $"{request.TenantId}/{root}/";
            var keys = await storage.ListAllKeysAsync(prefix, ct);

            foreach (var key in keys.Where(k => k.EndsWith(".md", StringComparison.OrdinalIgnoreCase)))
            {
                var obj = await storage.DownloadTextAsync(key, ct);
                if (obj is null) continue;
                fileCount++;

                var displayPath = "/" + (string.IsNullOrWhiteSpace(request.TenantId)
                    ? key : key[(request.TenantId!.Length + 1)..]);

                var lines = obj.Content.Split('\n');
                for (var i = 0; i < lines.Length; i++)
                {
                    if (!regex.IsMatch(lines[i])) continue;
                    matches.Add(new GrepMatchDto(displayPath, i + 1, lines[i].TrimEnd('\r')));
                    if (matches.Count >= request.MaxResults) goto done;
                }
            }
        }

    done:
        return new GrepResultDto(request.Query, fileCount, matches.Count, matches);
    }
}
