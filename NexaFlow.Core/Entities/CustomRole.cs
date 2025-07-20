using NexaFlow.Core.Common;

namespace NexaFlow.Core.Entities;

public class CustomRole : ITenantEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    
    public ICollection<RolePermission> Permissions { get; set; } = new List<RolePermission>();
}
