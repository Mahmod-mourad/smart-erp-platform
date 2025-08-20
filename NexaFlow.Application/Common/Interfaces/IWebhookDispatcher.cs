namespace NexaFlow.Application.Common.Interfaces;

public interface IWebhookDispatcher
{
    Task EnqueueWebhookAsync(Guid tenantId, string eventName, object payload, CancellationToken cancellationToken = default);
}
