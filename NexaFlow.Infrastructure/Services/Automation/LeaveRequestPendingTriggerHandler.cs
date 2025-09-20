using Microsoft.EntityFrameworkCore;
using NexaFlow.Core.Entities;
using NexaFlow.Core.Enums;
using NexaFlow.Infrastructure.Persistence;

namespace NexaFlow.Infrastructure.Services.Automation;

/// <summary>Fires when there are leave requests still awaiting review.</summary>
public class LeaveRequestPendingTriggerHandler(AppDbContext db) : ITriggerHandler
{
    public TriggerType TriggerType => TriggerType.LeaveRequestPending;

    public async Task<TriggerResult?> EvaluateAsync(WorkflowRule rule, CancellationToken ct)
    {
        var pending = await db.LeaveRequests
            .Where(l => l.Status == LeaveStatus.Pending)
            .CountAsync(ct);

        if (pending == 0)
            return null;

        return TriggerResult.From(
            $"{pending} leave request(s) awaiting review",
            new Dictionary<string, object> { ["pendingCount"] = pending });
    }
}
