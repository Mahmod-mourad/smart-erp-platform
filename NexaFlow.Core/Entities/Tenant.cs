using NexaFlow.Core.Common;
using NexaFlow.Core.Enums;

namespace NexaFlow.Core.Entities;

/// <summary>A customer company. Root of the multi-tenant isolation boundary.</summary>
public class Tenant : BaseEntity
{
    public required string Name { get; set; }

    /// <summary>URL/subdomain-safe unique identifier (e.g. "acme-corp").</summary>
    public required string Slug { get; set; }

    public TenantStatus Status { get; set; } = TenantStatus.PendingSetup;
    public SubscriptionPlan Plan { get; set; } = SubscriptionPlan.Free;
}
