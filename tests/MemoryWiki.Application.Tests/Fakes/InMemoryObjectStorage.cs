using System.Collections.Concurrent;
using System.Text;
using MemoryWiki.Application.Common.Interfaces;
using MemoryWiki.Domain.Entities;

namespace MemoryWiki.Application.Tests.Fakes;

/// <summary>In-memory IObjectStorage for fast, hermetic unit tests.</summary>
public sealed class InMemoryObjectStorage : IObjectStorage, ITranscriptContentReader
{
    private readonly ConcurrentDictionary<string, string> _store = new();

    public Task EnsureBucketAsync(CancellationToken ct = default) => Task.CompletedTask;

    public Task UploadTextAsync(string key, string content, string contentType = "text/markdown", CancellationToken ct = default)
    {
        _store[key] = content;
        return Task.CompletedTask;
    }

    public Task<StoredObject?> DownloadTextAsync(string key, CancellationToken ct = default) =>
        Task.FromResult(_store.TryGetValue(key, out var c)
            ? new StoredObject(key, c, Encoding.UTF8.GetByteCount(c), DateTimeOffset.UtcNow)
            : null);

    public Task<bool> ExistsAsync(string key, CancellationToken ct = default) => Task.FromResult(_store.ContainsKey(key));

    public Task<IReadOnlyList<MemoryNode>> ListAsync(string prefix, CancellationToken ct = default)
    {
        var nodes = _store.Keys
            .Where(k => k.StartsWith(prefix, StringComparison.Ordinal))
            .Select(k => new MemoryNode(k.Split('/').Last(), "/" + k, false, _store[k].Length, DateTimeOffset.UtcNow))
            .ToList();
        return Task.FromResult<IReadOnlyList<MemoryNode>>(nodes);
    }

    public Task<IReadOnlyList<string>> ListAllKeysAsync(string prefix, CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<string>>(_store.Keys.Where(k => k.StartsWith(prefix, StringComparison.Ordinal)).ToList());

    public Task<string?> ReadAsync(string objectKey, CancellationToken ct = default) =>
        Task.FromResult(_store.TryGetValue(objectKey, out var c) ? c : null);

    public int Count => _store.Count;
}
