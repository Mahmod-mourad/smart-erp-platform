using NexaFlow.Core.Common;
using NexaFlow.Core.Enums;

namespace NexaFlow.Core.Entities;

/// <summary>A CRM customer/account, owned by a tenant and optionally assigned to a user.</summary>
public class Customer : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }

    public required string Name { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Company { get; set; }
    public string? Notes { get; set; }

    public CustomerStatus Status { get; set; } = CustomerStatus.Active;

    /// <summary>The tenant user this customer is assigned to (an ApplicationUser id). Optional.</summary>
    public Guid? AssignedToId { get; set; }

    public ICollection<Lead> Leads { get; set; } = new List<Lead>();

    public ICollection<Activity> Activities { get; set; } = new List<Activity>();
}
