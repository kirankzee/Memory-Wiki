using System.Security.Claims;

namespace MemoryWiki.Api.Common;

public static class HttpContextExtensions
{
    /// <summary>Resolves the active tenant from the JWT "tenant" claim or the X-Tenant-Id header.</summary>
    public static string? ResolveTenant(this HttpContext http)
    {
        var claim = http.User.FindFirstValue("tenant");
        if (!string.IsNullOrWhiteSpace(claim)) return claim;
        var header = http.Request.Headers["X-Tenant-Id"].FirstOrDefault();
        return string.IsNullOrWhiteSpace(header) ? null : header;
    }
}
