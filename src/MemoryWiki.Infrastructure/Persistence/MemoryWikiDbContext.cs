using MemoryWiki.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace MemoryWiki.Infrastructure.Persistence;

public sealed class MemoryWikiDbContext(DbContextOptions<MemoryWikiDbContext> options) : DbContext(options)
{
    public DbSet<Transcript> Transcripts => Set<Transcript>();
    public DbSet<ProcessingJob> ProcessingJobs => Set<ProcessingJob>();
    public DbSet<MemoryDocument> MemoryDocuments => Set<MemoryDocument>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<Transcript>(e =>
        {
            e.ToTable("transcripts");
            e.HasKey(x => x.Id);
            e.Property(x => x.Title).HasMaxLength(300).IsRequired();
            e.Property(x => x.ObjectKey).HasMaxLength(512).IsRequired();
            e.Property(x => x.ContentHash).HasMaxLength(64);
            e.Property(x => x.Status).HasConversion<int>();
            e.Property(x => x.TenantId).HasMaxLength(64);
            e.Ignore(x => x.DomainEvents);
            e.Ignore(x => x.TranscriptId); // computed value object, not persisted
            e.HasIndex(x => x.TenantId);
        });

        b.Entity<ProcessingJob>(e =>
        {
            e.ToTable("processing_jobs");
            e.HasKey(x => x.Id);
            e.Property(x => x.Status).HasConversion<int>();
            e.Property(x => x.IdempotencyKey).HasMaxLength(64);
            e.HasIndex(x => x.TranscriptId).IsUnique();
        });

        b.Entity<MemoryDocument>(e =>
        {
            e.ToTable("memory_documents");
            e.HasKey(x => x.Id);
            e.Property(x => x.Path).HasMaxLength(512).IsRequired();
            e.Property(x => x.ObjectKey).HasMaxLength(512).IsRequired();
            e.Property(x => x.Title).HasMaxLength(300);
            e.Property(x => x.Slug).HasMaxLength(300);
            e.Property(x => x.Type).HasConversion<int>();
            e.Property(x => x.ContentHash).HasMaxLength(64);
            e.Property(x => x.TenantId).HasMaxLength(64);
            e.HasIndex(x => x.Path).IsUnique();
            e.HasIndex(x => new { x.Type, x.IsDeleted });
        });

        b.Entity<AuditLog>(e =>
        {
            e.ToTable("audit_logs");
            e.HasKey(x => x.Id);
            e.Property(x => x.Action).HasMaxLength(64);
            e.Property(x => x.EntityType).HasMaxLength(128);
            e.Property(x => x.EntityId).HasMaxLength(64);
            e.HasIndex(x => x.CreatedAtUtc);
        });
    }
}
