namespace NexaFlow.Infrastructure.Services.Automation;

/// <summary>
/// Executes a single action of a fired rule. One handler per action type (matched by
/// <see cref="ActionType"/> against the "type" field in the action's JSON config).
/// </summary>
public interface IActionHandler
{
    string ActionType { get; }

    Task<ActionResult> ExecuteAsync(
        string actionConfig,         // raw JSON for this action element
        TriggerResult triggerResult, // data from the trigger that fired
        Guid tenantId,
        CancellationToken ct);
}

public record ActionResult(bool Success, string Message)
{
    public static ActionResult Ok(string message) => new(true, message);
    public static ActionResult Fail(string message) => new(false, message);
}

/// <summary>Substitutes the supported placeholders ({{summary}}, {{timestamp}}) in a message body.</summary>
internal static class ActionTemplates
{
    public static string Apply(string template, TriggerResult trigger) => template
        .Replace("{{summary}}", trigger.Summary)
        .Replace("{{timestamp}}", DateTime.UtcNow.AddHours(3).ToString("dd/MM/yyyy HH:mm"));
}
