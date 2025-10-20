using NexaFlow.Application.Common.Interfaces;
using NexaFlow.Core.Constants;

namespace NexaFlow.API.Middleware;

/// <summary>
/// Resolves the tenant for the request from the authenticated JWT's <c>tenant_id</c> claim
/// and pushes it into the scoped <see cref="ITenantContext"/>, which the DbContext global
/// query filter consumes. Anonymous requests leave the tenant unset (cross-tenant lookups
/// allowed only for unauthenticated auth flows). (T-004)
/// </summary>
public class TenantResolutionMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context, ITenantContext tenantContext)
    {
        var claim = context.User.FindFirst(AppClaims.TenantId)?.Value;
        if (Guid.TryParse(claim, out var tenantId))
            tenantContext.SetTenant(tenantId);

        await next(context);
    }
}
