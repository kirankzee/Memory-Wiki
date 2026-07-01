using MemoryWiki.Domain.Entities;
using MemoryWiki.Domain.Repositories;

namespace MemoryWiki.Application.Tests.Fakes;

public sealed class FakeTranscriptRepository : ITranscriptRepository
{
    public readonly Dictionary<Guid, Transcript> Items = new();
    public Task AddAsync(Transcript t, CancellationToken ct = default) { Items[t.Id] = t; return Task.CompletedTask; }
    public Task<Transcript?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        Task.FromResult(Items.GetValueOrDefault(id));
}

public sealed class FakeJobRepository : IJobRepository
{
    public readonly List<ProcessingJob> Items = new();
    public Task AddAsync(ProcessingJob j, CancellationToken ct = default) { Items.Add(j); return Task.CompletedTask; }
    public Task<ProcessingJob?> GetByTranscriptIdAsync(Guid id, CancellationToken ct = default) =>
        Task.FromResult(Items.FirstOrDefault(j => j.TranscriptId == id));
}

public sealed class FakeMemoryRepository : IMemoryRepository
{
    public readonly Dictionary<string, MemoryDocument> Items = new();
    public Task<MemoryDocument?> GetByPathAsync(string path, CancellationToken ct = default) =>
        Task.FromResult(Items.GetValueOrDefault(path));
    public Task UpsertAsync(MemoryDocument d, CancellationToken ct = default) { Items[d.Path] = d; return Task.CompletedTask; }
    public Task<IReadOnlyList<MemoryDocument>> ListAllAsync(CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<MemoryDocument>>(Items.Values.ToList());
}

public sealed class FakeUnitOfWork : IUnitOfWork
{
    public int SaveCount { get; private set; }
    public Task<int> SaveChangesAsync(CancellationToken ct = default) { SaveCount++; return Task.FromResult(0); }
}
