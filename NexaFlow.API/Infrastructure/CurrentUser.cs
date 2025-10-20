using System.Security.Claims;
using NexaFlow.Application.Common.Interfaces;
using NexaFlow.Core.Constants;

namespace NexaFlow.API.Infrastructure;

/// <summary>Reads the authenticated principal from the current HTTP request's JWT claims.</summary>
public class CurrentUser(IHttpContextAccessor accessor) : ICurrentUser
{
    private ClaimsPrincipal? Principal => accessor.HttpContext?.User;

    public Guid? UserId =>
        Guid.TryParse(Principal?.FindFirstValue(ClaimTypes.NameIdentifier)
                      ?? Principal?.FindFirstValue("sub"), out var id) ? id : null;

    public Guid? TenantId =>
        Guid.TryParse(Principal?.FindFirstValue(AppClaims.TenantId), out var id) ? id : null;

    public string? Email => Principal?.FindFirstValue(ClaimTypes.Email)
                            ?? Principal?.FindFirstValue("email");

    public bool IsAuthenticated => Principal?.Identity?.IsAuthenticated ?? false;

    public bool IsInRole(string role) => Principal?.IsInRole(role) ?? false;
}
