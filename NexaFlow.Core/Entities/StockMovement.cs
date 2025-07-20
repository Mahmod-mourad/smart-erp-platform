using NexaFlow.Core.Common;
using NexaFlow.Core.Enums;

namespace NexaFlow.Core.Entities;

/// <summary>A stock In/Out event against a product, forming the audit trail for stock levels.</summary>
public class StockMovement : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }

    public Guid ProductId { get; set; }
    public Product Product { get; set; } = null!;

    public StockMovementType Type { get; set; }
    public int Quantity { get; set; }
    public string Reason { get; set; } = string.Empty;

    /// <summary>The Identity user (ApplicationUser id) who recorded the movement.</summary>
    public Guid CreatedById { get; set; }
}
