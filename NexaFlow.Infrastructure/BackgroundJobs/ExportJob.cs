using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NexaFlow.Application.Common.Interfaces;
using NexaFlow.Core.Common;
using NexaFlow.Infrastructure.Persistence;

namespace NexaFlow.Infrastructure.BackgroundJobs;

public class ExportJob
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IExportService _exportService;
    private readonly IExportNotifier _notifier;
    private readonly IStorageService _storageService;
    private readonly ILogger<ExportJob> _logger;

    public ExportJob(
        IServiceProvider serviceProvider,
        IExportService exportService,
        IStorageService storageService,
        IExportNotifier notifier,
        ILogger<ExportJob> logger)
    {
        _serviceProvider = serviceProvider;
        _exportService = exportService;
        _storageService = storageService;
        _notifier = notifier;
        _logger = logger;
    }

    public async Task RunExportAsync(Guid tenantId, Guid userId, string entityType, string format, CancellationToken cancellationToken)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            // Temporarily skip tenant filter for the background job to query specifically for the tenantId requested
            var data = await GetDataAsync(db, tenantId, entityType, cancellationToken);

            var dataBytes = format.ToLower() switch
            {
                "excel" => await _exportService.ExportToExcelAsync(data, cancellationToken: cancellationToken),
                _ => await _exportService.ExportToCsvAsync(data, cancellationToken)
            };

            var ext = format.ToLower() == "excel" ? "xlsx" : "csv";
            var contentType = format.ToLower() == "excel" ? "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet" : "text/csv";
            var fileName = $"{entityType}_export_{DateTime.UtcNow:yyyyMMddHHmmss}.{ext}";

            using var stream = new MemoryStream(dataBytes);
            var fileUrl = await _storageService.UploadFileAsync("exports", fileName, stream, contentType, cancellationToken);

            await _notifier.NotifyExportCompletedAsync(userId, entityType, fileUrl);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Export failed for user {UserId} and entity {EntityType}", userId, entityType);
            await _notifier.NotifyExportFailedAsync(userId, entityType, ex.Message);
            throw; // Let Hangfire know it failed
        }
    }

    private async Task<IEnumerable<object>> GetDataAsync(AppDbContext db, Guid tenantId, string entityType, CancellationToken ct)
    {
        return entityType.ToLowerInvariant() switch
        {
            "customers" => await db.Customers.IgnoreQueryFilters().Where(x => x.TenantId == tenantId).ToListAsync(ct),
            "journalentries" => await db.JournalEntries.IgnoreQueryFilters().Where(x => x.TenantId == tenantId).ToListAsync(ct),
            "accounts" => await db.Accounts.IgnoreQueryFilters().Where(x => x.TenantId == tenantId).ToListAsync(ct),
            "employees" => await db.Employees.IgnoreQueryFilters().Where(x => x.TenantId == tenantId).ToListAsync(ct),
            _ => throw new ArgumentException($"Unknown entity type {entityType}")
        };
    }
}
