using MemoryWiki.Domain.Entities;

namespace MemoryWiki.Domain.Repositories;

public interface ITranscriptRepository
{
    Task AddAsync(Transcript transcript, CancellationToken ct = default);
    Task<Transcript?> GetByIdAsync(Guid id, CancellationToken ct = default);
}

public interface IJobRepository
{
    Task AddAsync(ProcessingJob job, CancellationToken ct = default);
    Task<ProcessingJob?> GetByTranscriptIdAsync(Guid transcriptId, CancellationToken ct = default);
}

public interface IMemoryRepository
{
    Task<MemoryDocument?> GetByPathAsync(string path, CancellationToken ct = default);
    Task UpsertAsync(MemoryDocument document, CancellationToken ct = default);
    Task<IReadOnlyList<MemoryDocument>> ListAllAsync(CancellationToken ct = default);
}

/// <summary>Transactional boundary across repositories.</summary>
public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
