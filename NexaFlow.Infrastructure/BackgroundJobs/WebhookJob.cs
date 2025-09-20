using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using NexaFlow.Application.Common.Interfaces;
using NexaFlow.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace NexaFlow.Infrastructure.BackgroundJobs;

public class WebhookJob(HttpClient httpClient, AppDbContext dbContext, ILogger<WebhookJob> logger)
{
    public async Task SendWebhookAsync(Guid tenantId, string eventName, object payload, CancellationToken cancellationToken)
    {
        var subscriptions = await dbContext.WebhookSubscriptions
            .Where(w => w.TenantId == tenantId && w.IsActive && (w.EventName == eventName || w.EventName == "*"))
            .ToListAsync(cancellationToken);

        if (!subscriptions.Any())
        {
            logger.LogInformation("No active webhook subscriptions found for Event: {EventName}, Tenant: {TenantId}", eventName, tenantId);
            return;
        }

        foreach (var sub in subscriptions)
        {
            logger.LogInformation("Dispatching webhook {EventName} for Tenant {TenantId} to {Url}", eventName, tenantId, sub.TargetUrl);

            try
            {
                var response = await httpClient.PostAsJsonAsync(sub.TargetUrl, payload, cancellationToken);
                response.EnsureSuccessStatusCode();
                logger.LogInformation("Webhook {EventName} delivered successfully to {Url}.", eventName, sub.TargetUrl);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to deliver webhook {EventName} for Tenant {TenantId} to {Url}", eventName, tenantId, sub.TargetUrl);
                // In a real app we might throw to retry, but for now we just log and continue
                // to avoid one failing webhook from blocking other valid webhooks.
            }
        }
    }
}
