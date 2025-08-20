namespace NexaFlow.Application.Common.Interfaces;

public interface IExportNotifier
{
    Task NotifyExportCompletedAsync(Guid userId, string entityType, string fileUrl);
    Task NotifyExportFailedAsync(Guid userId, string entityType, string errorMessage);
}
