using System.Text.Json;
using NexaFlow.Application.DTOs;
using NexaFlow.Core.Entities;
using NexaFlow.Core.Enums;

namespace NexaFlow.Infrastructure.Services.Automation;

/// <summary>
/// Fires once per day at/after the configured Cairo-local time. The engine polls every few
/// minutes, so we guard against re-firing using <see cref="WorkflowRule.LastExecutedAt"/>.
/// </summary>
public class ScheduledDailyTriggerHandler : ITriggerHandler
{
    public TriggerType TriggerType => TriggerType.ScheduledDaily;

    public Task<TriggerResult?> EvaluateAsync(WorkflowRule rule, CancellationToken ct)
    {
        var config = JsonSerializer.Deserialize<ScheduledDailyTriggerConfig>(rule.TriggerConfig, AutomationJson.Options)
                     ?? new ScheduledDailyTriggerConfig(0, 0);

        var cairoNow = DateTime.UtcNow.AddHours(3);
        var scheduledToday = cairoNow.Date.AddHours(config.Hour).AddMinutes(config.Minute);

        if (cairoNow < scheduledToday)
            return Task.FromResult<TriggerResult?>(null); // not yet time today

        // Already fired today? (compare in Cairo-local terms)
        if (rule.LastExecutedAt is { } last && last.AddHours(3).Date == cairoNow.Date)
            return Task.FromResult<TriggerResult?>(null);

        var result = TriggerResult.From(
            $"Scheduled daily trigger at {config.Hour:D2}:{config.Minute:D2}");
        return Task.FromResult<TriggerResult?>(result);
    }
}
