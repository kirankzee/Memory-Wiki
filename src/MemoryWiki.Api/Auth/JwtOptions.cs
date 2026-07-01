namespace MemoryWiki.Api.Auth;

public sealed class JwtOptions
{
    public const string Section = "Jwt";
    public string SigningKey { get; set; } = "super-secret-development-signing-key-change-me-please-32+";
    public string Issuer { get; set; } = "memorywiki";
    public string Audience { get; set; } = "memorywiki-clients";
    public int ExpiryMinutes { get; set; } = 120;
}
