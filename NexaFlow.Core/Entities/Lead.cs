using NexaFlow.Core.Common;
using NexaFlow.Core.Enums;

namespace NexaFlow.Core.Entities;

/// <summary>A sales opportunity attached to a customer, moving through the pipeline stages.</summary>
public class Lead : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }

    public required string Title { get; set; }
    public decimal Value { get; set; }

    public LeadStage Stage { get; set; } = LeadStage.Prospect;

    public Guid CustomerId { get; set; }
    public Customer? Customer { get; set; }

    /// <summary>The tenant user this lead is assigned to (an ApplicationUser id). Optional.</summary>
    public Guid? AssignedToId { get; set; }

    public DateTime? ExpectedCloseDate { get; set; }
}
