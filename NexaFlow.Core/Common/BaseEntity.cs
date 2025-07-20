namespace NexaFlow.Core.Common;

/// <summary>Base for every persisted entity (Guid key + audit timestamps).</summary>
public abstract class BaseEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}

/// <summary>Marks an entity as belonging to a tenant — enforced by a global query filter.</summary>
public interface ITenantEntity
{
    Guid TenantId { get; set; }
}
