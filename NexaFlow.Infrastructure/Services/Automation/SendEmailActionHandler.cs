using System.Text.Json;
using Microsoft.Extensions.Logging;
using NexaFlow.Application.Common.Interfaces;
using NexaFlow.Application.DTOs;

namespace NexaFlow.Infrastructure.Services.Automation;

public class SendEmailActionHandler(IEmailSender email, ILogger<SendEmailActionHandler> logger) : IActionHandler
{
    public string ActionType => "SendEmail";

    public async Task<ActionResult> ExecuteAsync(
        string actionConfig, TriggerResult triggerResult, Guid tenantId, CancellationToken ct)
    {
        try
        {
            var action = JsonSerializer.Deserialize<SendEmailAction>(actionConfig, AutomationJson.Options)
                         ?? throw new InvalidOperationException("Invalid SendEmail config.");

            var body = ActionTemplates.Apply(action.Body, triggerResult);
            await email.SendAsync(action.To, action.Subject, body, ct);
            return ActionResult.Ok($"Email sent to {action.To}");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Automation: SendEmail action failed");
            return ActionResult.Fail($"Email failed: {ex.Message}");
        }
    }
}
