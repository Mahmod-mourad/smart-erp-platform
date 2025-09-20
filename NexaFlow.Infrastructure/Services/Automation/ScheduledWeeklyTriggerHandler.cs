using System.Text.Json;
using NexaFlow.Application.DTOs;
using NexaFlow.Core.Entities;
using NexaFlow.Core.Enums;

namespace NexaFlow.Infrastructure.Services.Automation;

/// <summary>Fires once a week on the configured day at/after the configured Cairo-local time.</summary>
public class ScheduledWeeklyTriggerHandler : ITriggerHandler
{
    public TriggerType TriggerType => TriggerType.ScheduledWeekly;

    public Task<TriggerResult?> EvaluateAsync(WorkflowRule rule, CancellationToken ct)
    {
        var config = JsonSerializer.Deserialize<ScheduledWeeklyTriggerConfig>(rule.TriggerConfig, AutomationJson.Options)
                     ?? new ScheduledWeeklyTriggerConfig(DayOfWeek.Sunday, 0, 0);

        var cairoNow = DateTime.UtcNow.AddHours(3);

        if (cairoNow.DayOfWeek != config.DayOfWeek)
            return Task.FromResult<TriggerResult?>(null);

        var scheduledToday = cairoNow.Date.AddHours(config.Hour).AddMinutes(config.Minute);
        if (cairoNow < scheduledToday)
            return Task.FromResult<TriggerResult?>(null);

        // Already fired today?
        if (rule.LastExecutedAt is { } last && last.AddHours(3).Date == cairoNow.Date)
            return Task.FromResult<TriggerResult?>(null);

        var result = TriggerResult.From(
            $"Scheduled weekly trigger ({config.DayOfWeek}) at {config.Hour:D2}:{config.Minute:D2}");
        return Task.FromResult<TriggerResult?>(result);
    }
}
