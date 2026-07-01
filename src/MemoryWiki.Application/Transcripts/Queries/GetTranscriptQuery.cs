using MediatR;
using MemoryWiki.Contracts.Dtos;
using MemoryWiki.Domain.Repositories;

namespace MemoryWiki.Application.Transcripts.Queries;

public sealed record GetTranscriptQuery(Guid Id) : IRequest<TranscriptDto?>;

public sealed class GetTranscriptHandler(ITranscriptRepository transcripts)
    : IRequestHandler<GetTranscriptQuery, TranscriptDto?>
{
    public async Task<TranscriptDto?> Handle(GetTranscriptQuery request, CancellationToken ct)
    {
        var t = await transcripts.GetByIdAsync(request.Id, ct);
        return t is null
            ? null
            : new TranscriptDto(t.Id, t.Title, t.Status.ToString(), t.SizeBytes, t.FailureReason, t.CreatedAtUtc, t.UpdatedAtUtc);
    }
}
