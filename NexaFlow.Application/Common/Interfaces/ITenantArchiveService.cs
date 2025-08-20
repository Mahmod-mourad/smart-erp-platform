namespace NexaFlow.Application.Common.Interfaces;

public interface ITenantArchiveService
{
    Task<string> CreateArchiveAsync(Guid tenantId, CancellationToken cancellationToken = default);
    Task RestoreArchiveAsync(Guid tenantId, Stream archiveStream, CancellationToken cancellationToken = default);
}
