using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using NexaFlow.Application.Common.Interfaces;
using NexaFlow.Core.Common;
using NexaFlow.Core.Entities;

namespace NexaFlow.Infrastructure.Persistence.Interceptors;

public class AuditInterceptor(ITenantContext tenantContext, ICurrentUser currentUser) : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        GenerateAuditLogs(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        GenerateAuditLogs(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void GenerateAuditLogs(DbContext? context)
    {
        if (context == null) return;
        
        var tenantId = tenantContext.TenantId;
        if (!tenantId.HasValue) return; // Do not log system-level migrations/seeders

        var userId = currentUser.UserId == Guid.Empty ? (Guid?)null : currentUser.UserId;

        context.ChangeTracker.DetectChanges();
        
        var auditEntries = new List<AuditLog>();

        foreach (var entry in context.ChangeTracker.Entries())
        {
            if (entry.Entity is AuditLog || entry.State == EntityState.Detached || entry.State == EntityState.Unchanged)
                continue;

            // Only audit tenant entities
            if (entry.Entity is not ITenantEntity)
                continue;

            var auditEntry = new AuditLog
            {
                TenantId = tenantId.Value,
                UserId = userId,
                EntityName = entry.Metadata.Name,
                Action = entry.State.ToString(),
                Timestamp = DateTime.UtcNow
            };

            // Get Primary Key
            var pk = entry.Properties.FirstOrDefault(p => p.Metadata.IsPrimaryKey());
            if (pk != null && pk.CurrentValue != null)
                auditEntry.EntityId = pk.CurrentValue.ToString()!;
            else
                auditEntry.EntityId = "Unknown";

            var oldValues = new Dictionary<string, object?>();
            var newValues = new Dictionary<string, object?>();

            foreach (var property in entry.Properties)
            {
                if (property.IsTemporary) continue; // Skip temporary values for inserts

                string propertyName = property.Metadata.Name;

                switch (entry.State)
                {
                    case EntityState.Added:
                        newValues[propertyName] = property.CurrentValue;
                        break;
                    case EntityState.Deleted:
                        oldValues[propertyName] = property.OriginalValue;
                        break;
                    case EntityState.Modified:
                        if (property.IsModified)
                        {
                            oldValues[propertyName] = property.OriginalValue;
                            newValues[propertyName] = property.CurrentValue;
                        }
                        break;
                }
            }

            auditEntry.OldValues = oldValues.Count > 0 ? JsonSerializer.Serialize(oldValues) : null;
            auditEntry.NewValues = newValues.Count > 0 ? JsonSerializer.Serialize(newValues) : null;
            
            auditEntries.Add(auditEntry);
        }

        if (auditEntries.Count > 0)
        {
            context.Set<AuditLog>().AddRange(auditEntries);
        }
    }
}
