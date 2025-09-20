using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using NexaFlow.Application.DTOs;
using NexaFlow.Core.Entities;
using NexaFlow.Core.Enums;
using NexaFlow.Infrastructure.Persistence;

namespace NexaFlow.Infrastructure.Services.Automation;

/// <summary>Fires when active employees have no attendance record for today past the deadline hour.</summary>
public class EmployeeAbsentTriggerHandler(AppDbContext db) : ITriggerHandler
{
    public TriggerType TriggerType => TriggerType.EmployeeAbsent;

    public async Task<TriggerResult?> EvaluateAsync(WorkflowRule rule, CancellationToken ct)
    {
        var config = JsonSerializer.Deserialize<EmployeeAbsentTriggerConfig>(rule.TriggerConfig, AutomationJson.Options)
                     ?? new EmployeeAbsentTriggerConfig(10, null);

        // Cairo local time (UTC+3) — attendance deadlines are expressed in business hours.
        var cairoNow = DateTime.UtcNow.AddHours(3);
        if (cairoNow.Hour < config.CheckDeadlineHour)
            return null; // too early to judge absence

        var today = DateOnly.FromDateTime(cairoNow);

        var activeEmployees = await db.Employees
            .Where(e => e.Status == EmployeeStatus.Active
                        && (config.EmployeeId == null || e.Id == config.EmployeeId))
            .Select(e => new { e.Id, e.FirstName, e.LastName })
            .ToListAsync(ct);

        if (activeEmployees.Count == 0)
            return null;

        var presentIds = await db.AttendanceRecords
            .Where(a => a.Date == today)
            .Select(a => a.EmployeeId)
            .ToListAsync(ct);
        var presentSet = presentIds.ToHashSet();

        var absent = activeEmployees
            .Where(e => !presentSet.Contains(e.Id))
            .Select(e => $"{e.FirstName} {e.LastName}".Trim())
            .ToList();

        if (absent.Count == 0)
            return null;

        return TriggerResult.From(
            $"{absent.Count} employee(s) absent today: {string.Join(", ", absent)}",
            new Dictionary<string, object>
            {
                ["absentCount"] = absent.Count,
                ["employees"] = absent
            });
    }
}
