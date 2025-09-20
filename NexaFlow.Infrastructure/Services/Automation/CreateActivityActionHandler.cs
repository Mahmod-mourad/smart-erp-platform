using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NexaFlow.Application.DTOs;
using NexaFlow.Core.Entities;
using NexaFlow.Infrastructure.Persistence;

namespace NexaFlow.Infrastructure.Services.Automation;

/// <summary>Logs a system-generated activity on a customer's timeline when a rule fires.</summary>
public class CreateActivityActionHandler(AppDbContext db, ILogger<CreateActivityActionHandler> logger) : IActionHandler
{
    public string ActionType => "CreateActivity";

    public async Task<ActionResult> ExecuteAsync(
        string actionConfig, TriggerResult triggerResult, Guid tenantId, CancellationToken ct)
    {
        try
        {
            var action = JsonSerializer.Deserialize<CreateActivityAction>(actionConfig, AutomationJson.Options)
                         ?? throw new InvalidOperationException("Invalid CreateActivity config.");

            // Customer read is tenant-scoped by the global query filter.
            var customerExists = await db.Customers.AnyAsync(c => c.Id == action.CustomerId, ct);
            if (!customerExists)
                return ActionResult.Fail($"CreateActivity failed: customer {action.CustomerId} not found");

            var content = $"{action.Subject} — {triggerResult.Summary}";
            if (content.Length > 2000) content = content[..2000];

            db.Activities.Add(new Activity
            {
                TenantId = tenantId,
                CustomerId = action.CustomerId,
                Type = action.ActivityType,
                Content = content,
                CreatedById = null // system-generated
            });
            await db.SaveChangesAsync(ct);

            return ActionResult.Ok($"Activity created on customer {action.CustomerId}");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Automation: CreateActivity action failed");
            return ActionResult.Fail($"CreateActivity failed: {ex.Message}");
        }
    }
}
