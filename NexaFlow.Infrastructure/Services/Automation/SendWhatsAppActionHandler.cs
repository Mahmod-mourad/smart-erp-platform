using System.Text.Json;
using Microsoft.Extensions.Logging;
using NexaFlow.Application.Common.Interfaces;
using NexaFlow.Application.DTOs;

namespace NexaFlow.Infrastructure.Services.Automation;

public class SendWhatsAppActionHandler(IWhatsAppSender whatsApp, ILogger<SendWhatsAppActionHandler> logger) : IActionHandler
{
    public string ActionType => "SendWhatsApp";

    public async Task<ActionResult> ExecuteAsync(
        string actionConfig, TriggerResult triggerResult, Guid tenantId, CancellationToken ct)
    {
        try
        {
            var action = JsonSerializer.Deserialize<SendWhatsAppAction>(actionConfig, AutomationJson.Options)
                         ?? throw new InvalidOperationException("Invalid SendWhatsApp config.");

            var message = ActionTemplates.Apply(action.Message, triggerResult);
            await whatsApp.SendMessageAsync(action.To, message, ct);
            return ActionResult.Ok($"WhatsApp sent to {action.To}");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Automation: SendWhatsApp action failed");
            return ActionResult.Fail($"WhatsApp failed: {ex.Message}");
        }
    }
}
