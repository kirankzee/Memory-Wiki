using MemoryWiki.Api.Auth;

namespace MemoryWiki.Api.Endpoints;

public sealed record TokenRequest(string Username, string Password);
public sealed record TokenResponse(string AccessToken, string TokenType, DateTime ExpiresUtc, string Role);

public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/auth").WithTags("Auth");

        group.MapPost("/token", (TokenRequest req, TokenService tokens) =>
        {
            var user = tokens.Validate(req.Username, req.Password);
            if (user is null) return Results.Unauthorized();

            var (token, expires) = tokens.Issue(user);
            return Results.Ok(new TokenResponse(token, "Bearer", expires, user.Role));
        })
        .WithName("IssueToken")
        .WithSummary("Exchange demo credentials (admin/admin or user/user) for a JWT.")
        .AllowAnonymous();

        return app;
    }
}
