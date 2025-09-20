using System.Text.Json;
using Microsoft.Extensions.Logging;
using NexaFlow.Application.Common.Interfaces;
using NexaFlow.Application.DTOs;

namespace NexaFlow.Infrastructure.Services.Automation;

public class SendSlackActionHandler(ISlackSender slack, ILogger<SendSlackActionHandler> logger) : IActionHandler
{
    public string ActionType => "SendSlack";

    public async Task<ActionResult> ExecuteAsync(
        string actionConfig, TriggerResult triggerResult, Guid tenantId, CancellationToken ct)
    {
        try
        {
            var action = JsonSerializer.Deserialize<SendSlackAction>(actionConfig, AutomationJson.Options)
                         ?? throw new InvalidOperationException("Invalid SendSlack config.");

            var message = ActionTemplates.Apply(action.Message, triggerResult);
            await slack.SendAsync(message, action.Channel, ct);
            return ActionResult.Ok(action.Channel is null
                ? "Slack message sent"
                : $"Slack message sent to {action.Channel}");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Automation: SendSlack action failed");
            return ActionResult.Fail($"Slack failed: {ex.Message}");
        }
    }
}
