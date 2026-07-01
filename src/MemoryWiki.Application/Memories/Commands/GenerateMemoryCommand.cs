using System.Security.Cryptography;
using System.Text;
using MediatR;
using MemoryWiki.Application.Common.Interfaces;
using MemoryWiki.Contracts.Messages;
using MemoryWiki.Domain.Entities;
using MemoryWiki.Domain.Enums;
using MemoryWiki.Domain.Repositories;
using MemoryWiki.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace MemoryWiki.Application.Memories.Commands;

/// <summary>
/// Core memory-generation pipeline, executed by the Worker for each queued transcript.
/// Idempotent: a transcript whose job already succeeded is a no-op. Existing memory
/// files are loaded and merged (not overwritten) so new transcripts enrich the wiki.
/// </summary>
public sealed record GenerateMemoryCommand(Guid TranscriptId, string ObjectKey, string IdempotencyKey, string? TenantId)
    : IRequest<int>;

public sealed class GenerateMemoryHandler(
    ITranscriptRepository transcripts,
    IJobRepository jobs,
    IMemoryRepository memories,
    IObjectStorage storage,
    ITranscriptContentReader contentReader,
    IGenerationService generation,
    IMessagePublisher publisher,
    IUnitOfWork unitOfWork,
    ILogger<GenerateMemoryHandler> logger)
    : IRequestHandler<GenerateMemoryCommand, int>
{
    private const int MaxAttempts = 5;

    public async Task<int> Handle(GenerateMemoryCommand request, CancellationToken ct)
    {
        var transcript = await transcripts.GetByIdAsync(request.TranscriptId, ct)
            ?? throw new InvalidOperationException($"Transcript {request.TranscriptId} not found.");

        var job = await jobs.GetByTranscriptIdAsync(request.TranscriptId, ct);
        if (job is null)
        {
            // Defensive: a message arrived without a pre-created job (e.g. replay). Track it.
            job = ProcessingJob.Create(request.TranscriptId, request.IdempotencyKey);
            await jobs.AddAsync(job, ct);
        }

        // Idempotency guard: do not reprocess an already-succeeded transcript.
        if (job.Status == JobStatus.Succeeded)
        {
            logger.LogInformation("Transcript {Id} already processed; skipping.", request.TranscriptId);
            return 0;
        }

        job.Start();
        transcript.MarkProcessing();
        await unitOfWork.SaveChangesAsync(ct);

        try
        {
            var text = await contentReader.ReadAsync(request.ObjectKey, ct)
                ?? throw new InvalidOperationException($"Transcript content missing at {request.ObjectKey}.");

            var extracted = await generation.ExtractAsync(transcript.Title, text, ct);
            var filesWritten = 0;

            foreach (var memory in extracted)
            {
                var slug = EntityName.Slugify(memory.Name);
                var path = MemoryPath.ForDocument(memory.Type, slug);
                var key = Scoped(path.ToObjectKey(), request.TenantId);

                var existing = await storage.DownloadTextAsync(key, ct);
                var markdown = await generation.ComposeMarkdownAsync(existing?.Content, memory, transcript.Title, ct);

                var hash = Hash(markdown);
                if (existing is not null && Hash(existing.Content) == hash)
                    continue; // nothing changed for this entity

                await storage.UploadTextAsync(key, markdown, "text/markdown", ct);

                var doc = await memories.GetByPathAsync(path.Value, ct);
                if (doc is null)
                {
                    doc = MemoryDocument.Create(path.Value, key, memory.Type, memory.Name, slug, hash,
                        Encoding.UTF8.GetByteCount(markdown), request.TenantId);
                }
                else
                {
                    doc.ApplyUpdate(hash, Encoding.UTF8.GetByteCount(markdown));
                }
                await memories.UpsertAsync(doc, ct);
                filesWritten++;
            }

            transcript.MarkCompleted(filesWritten);
            job.Succeed();
            await unitOfWork.SaveChangesAsync(ct);

            await publisher.PublishCompletedAsync(
                new MemoryCompletedMessage { TranscriptId = transcript.Id, FilesWritten = filesWritten }, ct);

            logger.LogInformation("Generated {Count} memory file(s) for transcript {Id}.", filesWritten, request.TranscriptId);
            return filesWritten;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Memory generation failed for transcript {Id} (attempt {Attempt}).",
                request.TranscriptId, job.Attempts);
            job.Fail(ex.Message, MaxAttempts);
            transcript.MarkFailed(ex.Message);
            await unitOfWork.SaveChangesAsync(ct);
            throw; // let the worker decide nack/retry vs. dead-letter
        }
    }

    private static string Scoped(string key, string? tenantId) =>
        string.IsNullOrWhiteSpace(tenantId) ? key : $"{tenantId}/{key}";

    private static string Hash(string content) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(content))).ToLowerInvariant();
}
