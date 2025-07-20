using NexaFlow.Core.Common;
using NexaFlow.Core.Enums;

namespace NexaFlow.Core.Entities;

/// <summary>
/// A no-code automation rule: "when [trigger] happens, run [actions]". The trigger and action
/// configs are stored as JSON so each trigger/action type can carry its own shape without
/// widening the table. Evaluated periodically by the automation engine (see AutomationService).
/// </summary>
public class WorkflowRule : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }

    /// <summary>Display name shown in the builder/list UI.</summary>
    public required string Name { get; set; }
    public string? Description { get; set; }

    /// <summary>Which condition fires this rule — selects the matching ITriggerHandler.</summary>
    public TriggerType TriggerType { get; set; }

    /// <summary>
    /// Trigger-specific config as JSON. Shape depends on <see cref="TriggerType"/>, e.g.
    /// StockLow: {"productId":"...","threshold":10}; ScheduledDaily: {"hour":8,"minute":0}.
    /// </summary>
    public string TriggerConfig { get; set; } = "{}";

    /// <summary>
    /// Ordered list of actions as a JSON array, each tagged with a "type", e.g.
    /// [{"type":"SendEmail","to":"...","subject":"...","body":"..."}].
    /// </summary>
    public string ActionsConfig { get; set; } = "[]";

    /// <summary>When false the engine skips the rule without deleting it.</summary>
    public bool IsActive { get; set; } = true;

    /// <summary>Last time the rule actually fired (trigger matched + actions ran).</summary>
    public DateTime? LastExecutedAt { get; set; }

    public ICollection<WorkflowLog> Logs { get; set; } = new List<WorkflowLog>();
}
