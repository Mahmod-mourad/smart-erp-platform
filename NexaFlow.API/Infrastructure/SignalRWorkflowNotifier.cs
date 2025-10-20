using Microsoft.AspNetCore.SignalR;
using NexaFlow.API.Hubs;
using NexaFlow.Application.Common.Interfaces;
using NexaFlow.Application.DTOs;

namespace NexaFlow.API.Infrastructure;

/// <summary>
/// SignalR-backed <see cref="IWorkflowNotifier"/>. Lives in the API layer because Infrastructure
/// must not depend on ASP.NET Core / SignalR — the engine talks to the abstraction instead.
/// </summary>
public class SignalRWorkflowNotifier(IHubContext<NotificationHub> hub) : IWorkflowNotifier
{
    public Task WorkflowExecutedAsync(Guid tenantId, WorkflowExecutedNotification payload, CancellationToken ct = default) =>
        hub.Clients.Group(tenantId.ToString()).SendAsync("WorkflowExecuted", payload, ct);
}
