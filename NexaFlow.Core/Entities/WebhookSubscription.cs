using NexaFlow.Core.Common;

namespace NexaFlow.Core.Entities;

public class WebhookSubscription : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    
    public string EventName { get; set; } = string.Empty; // e.g., "Leave.Approved", "Customer.Created", or "*"
    public string TargetUrl { get; set; } = string.Empty;
    public string? Secret { get; set; }
    public bool IsActive { get; set; } = true;
}
