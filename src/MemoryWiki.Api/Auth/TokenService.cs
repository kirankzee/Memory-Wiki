using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace MemoryWiki.Api.Auth;

public sealed record AuthUser(string Username, string Password, string Role, string? TenantId);

/// <summary>
/// Issues HS256 JWTs. Users are seeded from configuration for the demo; swap for an
/// identity provider (OIDC/Entra/Auth0) in production.
/// </summary>
public sealed class TokenService(IOptions<JwtOptions> options)
{
    private readonly JwtOptions _opt = options.Value;

    private static readonly AuthUser[] SeedUsers =
    {
        new("admin", "admin", "Admin", null),
        new("user", "user", "User", null)
    };

    public AuthUser? Validate(string username, string password) =>
        SeedUsers.FirstOrDefault(u =>
            u.Username.Equals(username, StringComparison.OrdinalIgnoreCase) && u.Password == password);

    public (string Token, DateTime ExpiresUtc) Issue(AuthUser user)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_opt.SigningKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expires = DateTime.UtcNow.AddMinutes(_opt.ExpiryMinutes);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Username),
            new(ClaimTypes.Name, user.Username),
            new(ClaimTypes.Role, user.Role),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };
        if (!string.IsNullOrWhiteSpace(user.TenantId))
            claims.Add(new Claim("tenant", user.TenantId));

        var token = new JwtSecurityToken(_opt.Issuer, _opt.Audience, claims,
            expires: expires, signingCredentials: creds);

        return (new JwtSecurityTokenHandler().WriteToken(token), expires);
    }
}
