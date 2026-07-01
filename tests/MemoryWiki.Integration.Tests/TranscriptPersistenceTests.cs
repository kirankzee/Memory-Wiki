using FluentAssertions;
using MemoryWiki.Domain.Entities;
using MemoryWiki.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;
using Xunit;

namespace MemoryWiki.Integration.Tests;

/// <summary>
/// Integration tests against a real PostgreSQL (via Testcontainers) that exercise the
/// EF Core mapping + InitialCreate migration and the repository round-trip.
/// Requires Docker to be running.
/// </summary>
public sealed class TranscriptPersistenceTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _pg = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .WithDatabase("memorywiki")
        .WithUsername("memorywiki")
        .WithPassword("memorywiki")
        .Build();

    private MemoryWikiDbContext NewContext() =>
        new(new DbContextOptionsBuilder<MemoryWikiDbContext>().UseNpgsql(_pg.GetConnectionString()).Options);

    public async Task InitializeAsync()
    {
        await _pg.StartAsync();
        await using var db = NewContext();
        await db.Database.MigrateAsync(); // applies InitialCreate against the container
    }

    public Task DisposeAsync() => _pg.DisposeAsync().AsTask();

    [Fact]
    public async Task Migration_creates_schema_and_repository_round_trips()
    {
        var transcript = Transcript.Create("Kickoff", "transcripts/x.txt", 42, "abc");
        transcript.MarkQueued();

        await using (var write = NewContext())
        {
            var repo = new TranscriptRepository(write);
            await repo.AddAsync(transcript);
            await new UnitOfWork(write).SaveChangesAsync();
        }

        await using var read = NewContext();
        var loaded = await new TranscriptRepository(read).GetByIdAsync(transcript.Id);

        loaded.Should().NotBeNull();
        loaded!.Title.Should().Be("Kickoff");
        loaded.Status.Should().Be(Domain.Enums.TranscriptStatus.Queued);
    }

    [Fact]
    public async Task ProcessingJob_has_unique_transcript_constraint()
    {
        var transcriptId = Guid.NewGuid();

        await using var db = NewContext();
        db.ProcessingJobs.Add(ProcessingJob.Create(transcriptId, "k1"));
        await db.SaveChangesAsync();

        db.ProcessingJobs.Add(ProcessingJob.Create(transcriptId, "k2"));
        var act = async () => await db.SaveChangesAsync();

        await act.Should().ThrowAsync<DbUpdateException>();
    }
}
