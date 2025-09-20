using Microsoft.AspNetCore.Identity;
using NexaFlow.Core.Common;
using NexaFlow.Core.Entities;

namespace NexaFlow.Infrastructure.Identity;

/// <summary>App user backed by ASP.NET Core Identity, scoped to a tenant.</summary>
public class ApplicationUser : IdentityUser<Guid>, ITenantEntity
{
    public Guid TenantId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastLoginAt { get; set; }

    public Guid? CustomRoleId { get; set; }
    public CustomRole? CustomRole { get; set; }

    public string FullName => $"{FirstName} {LastName}".Trim();
}

/// <summary>Role; TenantId is null for platform-level (system) roles like SuperAdmin.</summary>
public class ApplicationRole : IdentityRole<Guid>
{
    public ApplicationRole() { }
    public ApplicationRole(string name) : base(name) { }

    public Guid? TenantId { get; set; }
}
