using NexaFlow.Core.Common;
using NexaFlow.Core.Enums;

namespace NexaFlow.Core.Entities;

/// <summary>
/// A timeline entry on a customer — a note, a logged call/email/meeting, or a
/// system-generated event (e.g. a lead changing stage). Drives the Customer Detail timeline.
/// </summary>
public class Activity : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }

    public Guid CustomerId { get; set; }
    public Customer? Customer { get; set; }

    public ActivityType Type { get; set; } = ActivityType.Note;

    public required string Content { get; set; }

    /// <summary>The tenant user who authored the entry (an ApplicationUser id). Null for system events.</summary>
    public Guid? CreatedById { get; set; }
}
