using System.Security.Cryptography;
using System.Text;
using FluentValidation;
using MediatR;
using MemoryWiki.Application.Common.Interfaces;
using MemoryWiki.Contracts.Dtos;
using MemoryWiki.Contracts.Messages;
using MemoryWiki.Domain.Entities;
using MemoryWiki.Domain.Repositories;

namespace MemoryWiki.Application.Transcripts.Commands;

public sealed record CreateTranscriptCommand(string Title, string Content, string? TenantId)
    : IRequest<CreateTranscriptResponse>;

public sealed class CreateTranscriptValidator : AbstractValidator<CreateTranscriptCommand>
{
    public const int MaxBytes = 5 * 1024 * 1024; // 5 MB upload limit

    public CreateTranscriptValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(300);
        RuleFor(x => x.Content).NotEmpty().WithMessage("Transcript content is empty.");
        RuleFor(x => x.Content)
            .Must(c => Encoding.UTF8.GetByteCount(c) <= MaxBytes)
            .WithMessage($"Transcript exceeds the {MaxBytes / (1024 * 1024)} MB limit.");
    }
}

public sealed class CreateTranscriptHandler(
    ITranscriptRepository transcripts,
    IJobRepository jobs,
    IObjectStorage storage,
    IMessagePublisher publisher,
    IUnitOfWork unitOfWork)
    : IRequestHandler<CreateTranscriptCommand, CreateTranscriptResponse>
{
    public async Task<CreateTranscriptResponse> Handle(CreateTranscriptCommand request, CancellationToken ct)
    {
        var bytes = Encoding.UTF8.GetBytes(request.Content);
        var hash = Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();

        var transcript = Transcript.Create(
            title: request.Title,
            objectKey: "placeholder",
            sizeBytes: bytes.LongLength,
            contentHash: hash,
            tenantId: request.TenantId);

        // Object key is derived from the generated id so the raw transcript is addressable.
        var objectKey = TranscriptKey(transcript.Id, request.TenantId);
        transcript.AssignObjectKey(objectKey);

        await storage.UploadTextAsync(objectKey, request.Content, "text/plain", ct);

        var job = ProcessingJob.Create(transcript.Id, idempotencyKey: hash);

        await transcripts.AddAsync(transcript, ct);
        await jobs.AddAsync(job, ct);
        transcript.MarkQueued();
        await unitOfWork.SaveChangesAsync(ct);

        await publisher.PublishGenerateAsync(new GenerateMemoryMessage
        {
            TranscriptId = transcript.Id,
            ObjectKey = objectKey,
            IdempotencyKey = hash,
            TenantId = request.TenantId
        }, ct);

        return new CreateTranscriptResponse(transcript.Id, transcript.Status.ToString());
    }

    public static string TranscriptKey(Guid id, string? tenantId) =>
        string.IsNullOrWhiteSpace(tenantId) ? $"transcripts/{id}.txt" : $"{tenantId}/transcripts/{id}.txt";
}
