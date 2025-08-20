namespace NexaFlow.Application.Common.Interfaces;

/// <summary>The authenticated principal for the current request (resolved from the JWT).</summary>
public interface ICurrentUser
{
    Guid? UserId { get; }
    Guid? TenantId { get; }
    string? Email { get; }
    bool IsAuthenticated { get; }
    bool IsInRole(string role);
}

/// <summary>
/// The active tenant for the current request. Consumed by the DbContext global query
/// filter so no query can leak across tenants. Resolved by the tenant middleware.
/// </summary>
public interface ITenantContext
{
    Guid? TenantId { get; }
    void SetTenant(Guid tenantId);
}
