using MediatR;
using MemoryWiki.Api.Common;
using MemoryWiki.Application.Memories.Queries;
using MemoryWiki.Contracts.Dtos;

namespace MemoryWiki.Api.Endpoints;

/// <summary>Unix-style REST API over the memory tree: ls / cat / grep.</summary>
public static class MemoryEndpoints
{
    public static IEndpointRouteBuilder MapMemoryEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/memory").WithTags("Memory").RequireAuthorization();

        // GET /api/memory/ls?path=/people
        group.MapGet("/ls", async (string? path, HttpContext http, IMediator mediator, CancellationToken ct) =>
        {
            var listing = await mediator.Send(new ListMemoryQuery(path ?? "/", http.ResolveTenant()), ct);
            return Results.Ok(listing);
        })
        .WithName("ListMemory")
        .WithSummary("List a directory in the memory tree (ls).")
        .Produces<DirectoryListingDto>();

        // GET /api/memory/cat?path=/people/john-doe.md
        group.MapGet("/cat", async (string path, HttpContext http, IMediator mediator, CancellationToken ct) =>
        {
            var file = await mediator.Send(new ReadMemoryQuery(path, http.ResolveTenant()), ct);
            if (file is null) return Results.NotFound(new { error = $"No memory file at '{path}'." });

            // Return raw markdown so it renders nicely in browsers/clients.
            return Results.Text(file.Content, "text/markdown");
        })
        .WithName("ReadMemory")
        .WithSummary("Read a memory file's markdown (cat).")
        .Produces(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound);

        // GET /api/memory/grep?q=RabbitMQ&ignoreCase=true
        group.MapGet("/grep", async (string q, bool? ignoreCase, int? max, HttpContext http, IMediator mediator, CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(q))
                return Results.BadRequest(new { error = "Query 'q' is required." });

            var result = await mediator.Send(
                new SearchMemoryQuery(q, http.ResolveTenant(), ignoreCase ?? true, max ?? 200), ct);
            return Results.Ok(result);
        })
        .WithName("GrepMemory")
        .WithSummary("Search across all memory files (grep).")
        .Produces<GrepResultDto>();

        return app;
    }
}
