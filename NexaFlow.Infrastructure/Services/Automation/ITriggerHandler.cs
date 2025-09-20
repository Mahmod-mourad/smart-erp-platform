using NexaFlow.Core.Entities;
using NexaFlow.Core.Enums;

namespace NexaFlow.Infrastructure.Services.Automation;

/// <summary>
/// Evaluates whether a rule's trigger condition currently holds. One handler per
/// <see cref="TriggerType"/>; the engine picks the matching handler for each rule.
/// </summary>
public interface ITriggerHandler
{
    TriggerType TriggerType { get; }

    /// <summary>Returns a <see cref="TriggerResult"/> when the condition is met, otherwise null.</summary>
    Task<TriggerResult?> EvaluateAsync(WorkflowRule rule, CancellationToken ct);
}

/// <summary>
/// Output of a fired trigger: a human-readable summary plus structured data that action
/// handlers can use for richer message bodies.
/// </summary>
public record TriggerResult(string Summary, IReadOnlyDictionary<string, object> Data)
{
    public static TriggerResult From(string summary, IReadOnlyDictionary<string, object>? data = null) =>
        new(summary, data ?? new Dictionary<string, object>());
}
