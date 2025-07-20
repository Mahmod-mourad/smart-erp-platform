using NexaFlow.Core.Common;

namespace NexaFlow.Core.Entities;

public class RolePermission : ITenantEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    
    public Guid CustomRoleId { get; set; }
    public CustomRole CustomRole { get; set; } = null!;
    
    public string Permission { get; set; } = null!;
}
