using Microsoft.AspNetCore.SignalR;
using NexaFlow.Application.Common.Interfaces;
using NexaFlow.API.Hubs;

namespace NexaFlow.API.Infrastructure;

public class SignalRExportNotifier(IHubContext<NotificationHub> hubContext) : IExportNotifier
{
    public async Task NotifyExportCompletedAsync(Guid userId, string entityType, string fileUrl)
    {
        await hubContext.Clients.User(userId.ToString()).SendAsync("ExportCompleted", new
        {
            EntityType = entityType,
            FileUrl = fileUrl,
            Message = $"Your {entityType} export is ready."
        });
    }

    public async Task NotifyExportFailedAsync(Guid userId, string entityType, string errorMessage)
    {
        await hubContext.Clients.User(userId.ToString()).SendAsync("ExportFailed", new
        {
            EntityType = entityType,
            Message = $"Export for {entityType} failed: {errorMessage}"
        });
    }
}
