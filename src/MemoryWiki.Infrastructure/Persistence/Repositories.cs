using MemoryWiki.Domain.Entities;
using MemoryWiki.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace MemoryWiki.Infrastructure.Persistence;

public sealed class TranscriptRepository(MemoryWikiDbContext db) : ITranscriptRepository
{
    public async Task AddAsync(Transcript transcript, CancellationToken ct = default) =>
        await db.Transcripts.AddAsync(transcript, ct);

    public Task<Transcript?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        db.Transcripts.FirstOrDefaultAsync(x => x.Id == id, ct);
}

public sealed class JobRepository(MemoryWikiDbContext db) : IJobRepository
{
    public async Task AddAsync(ProcessingJob job, CancellationToken ct = default) =>
        await db.ProcessingJobs.AddAsync(job, ct);

    public Task<ProcessingJob?> GetByTranscriptIdAsync(Guid transcriptId, CancellationToken ct = default) =>
        db.ProcessingJobs.FirstOrDefaultAsync(x => x.TranscriptId == transcriptId, ct);
}

public sealed class MemoryRepository(MemoryWikiDbContext db) : IMemoryRepository
{
    public Task<MemoryDocument?> GetByPathAsync(string path, CancellationToken ct = default) =>
        db.MemoryDocuments.FirstOrDefaultAsync(x => x.Path == path, ct);

    public async Task UpsertAsync(MemoryDocument document, CancellationToken ct = default)
    {
        var exists = await db.MemoryDocuments.AnyAsync(x => x.Id == document.Id, ct);
        if (!exists) await db.MemoryDocuments.AddAsync(document, ct);
    }

    public async Task<IReadOnlyList<MemoryDocument>> ListAllAsync(CancellationToken ct = default) =>
        await db.MemoryDocuments.Where(x => !x.IsDeleted).ToListAsync(ct);
}

public sealed class UnitOfWork(MemoryWikiDbContext db) : IUnitOfWork
{
    public Task<int> SaveChangesAsync(CancellationToken ct = default) => db.SaveChangesAsync(ct);
}
