using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace MemoryWiki.Api.Common;

/// <summary>Translates exceptions into RFC-7807 ProblemDetails responses.</summary>
public sealed class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken ct)
    {
        var (status, title, errors) = exception switch
        {
            ValidationException ve => (StatusCodes.Status400BadRequest, "Validation failed",
                ve.Errors.Select(e => $"{e.PropertyName}: {e.ErrorMessage}").ToArray()),
            ArgumentException ae => (StatusCodes.Status400BadRequest, "Invalid request", new[] { ae.Message }),
            KeyNotFoundException => (StatusCodes.Status404NotFound, "Not found", Array.Empty<string>()),
            InvalidOperationException io => (StatusCodes.Status409Conflict, "Conflict", new[] { io.Message }),
            _ => (StatusCodes.Status500InternalServerError, "An unexpected error occurred", Array.Empty<string>())
        };

        if (status == StatusCodes.Status500InternalServerError)
            logger.LogError(exception, "Unhandled exception");

        var problem = new ProblemDetails
        {
            Status = status,
            Title = title,
            Type = $"https://httpstatuses.com/{status}",
            Detail = errors.Length > 0 ? string.Join("; ", errors) : exception.Message
        };
        problem.Extensions["traceId"] = context.TraceIdentifier;
        if (errors.Length > 0) problem.Extensions["errors"] = errors;

        context.Response.StatusCode = status;
        await context.Response.WriteAsJsonAsync(problem, ct);
        return true;
    }
}
