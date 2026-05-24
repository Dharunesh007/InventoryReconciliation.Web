using System.Security.Claims;
using InventoryReconciliation.Application.Abstractions;
using Microsoft.AspNetCore.Http;

namespace InventoryReconciliation.Infrastructure.Security;

public sealed class CurrentUserService(IHttpContextAccessor httpContextAccessor) : ICurrentUserService
{
    public string UserId =>
        httpContextAccessor.HttpContext?.User.FindFirstValue("oid")
        ?? httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? "system";

    public string DisplayName =>
        httpContextAccessor.HttpContext?.User.Identity?.Name
        ?? httpContextAccessor.HttpContext?.User.FindFirstValue("name")
        ?? "System";

    public bool IsInRole(string role) =>
        httpContextAccessor.HttpContext?.User.IsInRole(role) == true;
}
