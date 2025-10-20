using NexaFlow.Application.Common.Interfaces;

namespace NexaFlow.API.Infrastructure;

/// <summary>Scoped holder for the active tenant. Populated by <see cref="Middleware.TenantResolutionMiddleware"/>.</summary>
public class TenantContext : ITenantContext
{
    public Guid? TenantId { get; private set; }

    public void SetTenant(Guid tenantId) => TenantId = tenantId;
}
