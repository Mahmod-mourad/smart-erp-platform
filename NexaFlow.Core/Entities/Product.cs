using NexaFlow.Core.Common;

namespace NexaFlow.Core.Entities;

/// <summary>An inventory product owned by a tenant. Stock level is driven by StockMovements.</summary>
public class Product : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }

    public required string Name { get; set; }
    public string? SKU { get; set; }
    public string? Category { get; set; }
    public decimal UnitPrice { get; set; }

    /// <summary>Current on-hand quantity; recomputed after every StockMovement.</summary>
    public int CurrentStock { get; set; }

    /// <summary>Threshold below which the product is flagged low-stock.</summary>
    public int MinimumStock { get; set; }

    /// <summary>Set whenever CurrentStock drops below MinimumStock (see InventoryService).</summary>
    public bool IsLowStock { get; set; }

    public string? Description { get; set; }

    public ICollection<StockMovement> Movements { get; set; } = new List<StockMovement>();
}
