using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NexaFlow.Application.Common.Interfaces;
using NexaFlow.Core.Enums;
using NexaFlow.Infrastructure.Persistence;

namespace NexaFlow.Infrastructure.Jobs;

/// <summary>
/// Recurring Hangfire job that drives the automation engine. There is no HTTP context here, so
/// for each active tenant it opens a fresh DI scope and sets the ambient
/// <see cref="ITenantContext"/> — that makes the DbContext global query filter scope every read
/// to that tenant, keeping isolation intact.
/// </summary>
public class AutomationJob(IServiceScopeFactory scopeFactory, ILogger<AutomationJob> logger)
{
    [AutomaticRetry(Attempts = 3)]
    public async Task RunForAllTenantsAsync(CancellationToken ct)
    {
        logger.LogInformation("Automation job started at {Time:o}", DateTime.UtcNow);

        List<Guid> tenantIds;
        await using (var scope = scopeFactory.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            tenantIds = await db.Tenants
                .Where(t => t.Status == TenantStatus.Active)
                .Select(t => t.Id)
                .ToListAsync(ct);
        }

        foreach (var tenantId in tenantIds)
        {
            try
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                scope.ServiceProvider.GetRequiredService<ITenantContext>().SetTenant(tenantId);
                var engine = scope.ServiceProvider.GetRequiredService<IAutomationService>();
                await engine.EvaluateActiveRulesAsync(ct);
            }
            catch (Exception ex)
            {
                // One tenant failing must not abort the others.
                logger.LogError(ex, "Automation job failed for tenant {TenantId}", tenantId);
            }
        }

        logger.LogInformation("Automation job completed at {Time:o}", DateTime.UtcNow);
    }
}
