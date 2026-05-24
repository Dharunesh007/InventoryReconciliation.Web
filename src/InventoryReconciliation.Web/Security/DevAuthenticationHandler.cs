using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace InventoryReconciliation.Web.Security;

public static class DevAuthenticationDefaults
{
    public const string Scheme = "Development";
}

public sealed class DevAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "dev-user"),
            new Claim("oid", "dev-user"),
            new Claim(ClaimTypes.Name, "Dev Inventory Admin"),
            new Claim(ClaimTypes.Email, "inventory.admin@example.com"),
            new Claim(ClaimTypes.Role, "SuperAdmin"),
            new Claim(ClaimTypes.Role, "InventoryAdmin"),
            new Claim(ClaimTypes.Role, "Auditor"),
            new Claim(ClaimTypes.Role, "RegionalManager"),
            new Claim(ClaimTypes.Role, "ComplianceTeam")
        };

        var identity = new ClaimsIdentity(claims, DevAuthenticationDefaults.Scheme);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, DevAuthenticationDefaults.Scheme);
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
