using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using NexaFlow.Application.DTOs;
using NexaFlow.Core.Entities;
using NexaFlow.Core.Enums;
using NexaFlow.Infrastructure.Persistence;

namespace NexaFlow.Infrastructure.Services.Automation;

/// <summary>Fires when one or more products are at/below their low-stock threshold.</summary>
public class StockLowTriggerHandler(AppDbContext db) : ITriggerHandler
{
    public TriggerType TriggerType => TriggerType.StockLow;

    public async Task<TriggerResult?> EvaluateAsync(WorkflowRule rule, CancellationToken ct)
    {
        var config = JsonSerializer.Deserialize<StockLowTriggerConfig>(rule.TriggerConfig, AutomationJson.Options)
                     ?? new StockLowTriggerConfig(null, 0);

        // Reads are auto-scoped to the current tenant by the DbContext global query filter.
        List<Product> lowStock;
        if (config.ProductId is { } productId)
        {
            var product = await db.Products.FirstOrDefaultAsync(p => p.Id == productId, ct);
            var threshold = config.Threshold > 0 ? config.Threshold : product?.MinimumStock ?? 0;
            lowStock = product is not null && product.CurrentStock <= threshold
                ? [product]
                : [];
        }
        else
        {
            // threshold == 0 means "use each product's own MinimumStock".
            lowStock = await db.Products
                .Where(p => p.CurrentStock <= (config.Threshold > 0 ? config.Threshold : p.MinimumStock))
                .OrderBy(p => p.Name)
                .ToListAsync(ct);
        }

        if (lowStock.Count == 0)
            return null;

        var detail = string.Join(", ", lowStock.Select(p => $"{p.Name}: {p.CurrentStock}/{p.MinimumStock}"));
        return TriggerResult.From(
            $"Low stock detected — {detail}",
            new Dictionary<string, object>
            {
                ["count"] = lowStock.Count,
                ["products"] = lowStock.Select(p => new { p.Name, p.CurrentStock, p.MinimumStock }).ToList()
            });
    }
}
