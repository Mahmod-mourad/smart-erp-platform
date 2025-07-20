using NexaFlow.Core.Common;

namespace NexaFlow.Core.Entities;

public class AuditLog : ITenantEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public Guid? UserId { get; set; }
    
    public string EntityName { get; set; } = null!;
    public string EntityId { get; set; } = null!;
    public string Action { get; set; } = null!; // "Insert", "Update", "Delete"
    
    public string? OldValues { get; set; } // JSON
    public string? NewValues { get; set; } // JSON
    
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
