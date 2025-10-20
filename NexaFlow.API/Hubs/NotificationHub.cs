using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using NexaFlow.Core.Constants;

namespace NexaFlow.API.Hubs;

/// <summary>
/// Real-time channel to the frontend. Each connection joins a group keyed by its tenant id, so
/// the automation engine can push workflow events to exactly one tenant's clients.
/// </summary>
[Authorize]
public class NotificationHub : Hub
{
    public override async Task OnConnectedAsync()
    {
        var tenantId = Context.User?.FindFirst(AppClaims.TenantId)?.Value;
        if (!string.IsNullOrEmpty(tenantId))
            await Groups.AddToGroupAsync(Context.ConnectionId, tenantId);

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var tenantId = Context.User?.FindFirst(AppClaims.TenantId)?.Value;
        if (!string.IsNullOrEmpty(tenantId))
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, tenantId);

        await base.OnDisconnectedAsync(exception);
    }
}
