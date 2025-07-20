using NexaFlow.Core.Common;
using NexaFlow.Core.Enums;

namespace NexaFlow.Core.Entities;

/// <summary>An audit record of one WorkflowRule execution — what fired and how each action fared.</summary>
public class WorkflowLog : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }

    public Guid RuleId { get; set; }
    public WorkflowRule Rule { get; set; } = null!;

    public DateTime ExecutedAt { get; set; } = DateTime.UtcNow;

    public WorkflowLogStatus Status { get; set; }

    /// <summary>Per-action outcome lines, e.g. "✅ Email sent to manager@co.com\n❌ WhatsApp failed: ...".</summary>
    public string Details { get; set; } = string.Empty;

    /// <summary>Human-readable summary of the trigger data that fired the rule.</summary>
    public string? TriggerData { get; set; }
}
