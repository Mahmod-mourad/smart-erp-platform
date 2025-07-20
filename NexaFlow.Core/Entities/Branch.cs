using NexaFlow.Core.Common;

namespace NexaFlow.Core.Entities;

/// <summary>
/// Represents a company branch (e.g., Headquarters, Riyadh Branch) belonging to a Tenant.
/// </summary>
public class Branch : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }

    public required string Name { get; set; }
    
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? Phone { get; set; }

    /// <summary>
    /// Indicates whether this is the main headquarters branch.
    /// </summary>
    public bool IsHeadquarters { get; set; } = false;

    // Navigation property
    public ICollection<Employee> Employees { get; set; } = new List<Employee>();
}
