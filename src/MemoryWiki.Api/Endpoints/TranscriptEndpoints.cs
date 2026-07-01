using System.Text;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using MemoryWiki.Api.Common;
using MemoryWiki.Application.Transcripts.Commands;
using MemoryWiki.Application.Transcripts.Queries;
using MemoryWiki.Contracts.Dtos;

namespace MemoryWiki.Api.Endpoints;

public static class TranscriptEndpoints
{
    public static IEndpointRouteBuilder MapTranscriptEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/transcripts").WithTags("Transcripts").RequireAuthorization();

        // POST /api/transcripts — multipart/form-data { title, file }
        group.MapPost("/", async (HttpContext http, IFormFile file, [FromForm] string? title, IMediator mediator) =>
        {
            if (file is null || file.Length == 0)
                return Results.BadRequest(new { error = "A non-empty 'file' is required." });

            using var reader = new StreamReader(file.OpenReadStream(), Encoding.UTF8);
            var content = await reader.ReadToEndAsync(http.RequestAborted);

            var effectiveTitle = string.IsNullOrWhiteSpace(title)
                ? Path.GetFileNameWithoutExtension(file.FileName)
                : title;

            var response = await mediator.Send(
                new CreateTranscriptCommand(effectiveTitle, content, http.ResolveTenant()), http.RequestAborted);

            return Results.Created($"/api/transcripts/{response.Id}", response);
        })
        .DisableAntiforgery()
        .WithName("CreateTranscript")
        .WithSummary("Ingest a transcript (multipart/form-data). Returns id + status.")
        .Produces<CreateTranscriptResponse>(StatusCodes.Status201Created)
        .Produces(StatusCodes.Status400BadRequest);

        // GET /api/transcripts/{id}
        group.MapGet("/{id:guid}", async (Guid id, IMediator mediator, CancellationToken ct) =>
        {
            var dto = await mediator.Send(new GetTranscriptQuery(id), ct);
            return dto is null ? Results.NotFound() : Results.Ok(dto);
        })
        .WithName("GetTranscript")
        .WithSummary("Get transcript status and metadata.")
        .Produces<TranscriptDto>()
        .Produces(StatusCodes.Status404NotFound);

        return app;
    }
}
