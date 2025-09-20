using System.Text.Json;
using Microsoft.Extensions.Logging;
using NexaFlow.Application.Common.Interfaces;
using NexaFlow.Application.DTOs;

namespace NexaFlow.Infrastructure.Services.Automation;

/// <summary>
/// Automation action that appends a row to the tenant's Google Sheet. The configured row is a
/// template that supports the usual {{summary}}/{{timestamp}} placeholders, split on tabs/pipes
/// into columns.
/// </summary>
public class AppendSheetActionHandler(IGoogleSheetsSender sheets, ILogger<AppendSheetActionHandler> logger) : IActionHandler
{
    public string ActionType => "AppendToSheet";

    public async Task<ActionResult> ExecuteAsync(
        string actionConfig, TriggerResult triggerResult, Guid tenantId, CancellationToken ct)
    {
        try
        {
            var action = JsonSerializer.Deserialize<AppendSheetAction>(actionConfig, AutomationJson.Options)
                         ?? throw new InvalidOperationException("Invalid AppendToSheet config.");

            var rendered = ActionTemplates.Apply(action.Row, triggerResult);
            var columns = rendered.Split('|').Select(c => c.Trim()).ToArray();

            await sheets.AppendRowAsync(columns, ct);
            return ActionResult.Ok("Row appended to Google Sheet");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Automation: AppendToSheet action failed");
            return ActionResult.Fail($"Google Sheets failed: {ex.Message}");
        }
    }
}
