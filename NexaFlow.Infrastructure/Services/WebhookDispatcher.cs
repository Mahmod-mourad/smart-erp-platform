using Hangfire;
using NexaFlow.Application.Common.Interfaces;

namespace NexaFlow.Infrastructure.Services;

public class WebhookDispatcher(IBackgroundJobClient backgroundJobClient) : IWebhookDispatcher
{
    public Task EnqueueWebhookAsync(Guid tenantId, string eventName, object payload, CancellationToken cancellationToken = default)
    {
        backgroundJobClient.Enqueue<BackgroundJobs.WebhookJob>(job => job.SendWebhookAsync(tenantId, eventName, payload, CancellationToken.None));
        return Task.CompletedTask;
    }
}
