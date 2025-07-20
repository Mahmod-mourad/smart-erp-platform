namespace NexaFlow.Core.Constants;

/// <summary>Canonical role names. Seeded at startup (see DbSeeder).</summary>
public static class AppRoles
{
    public const string SuperAdmin = "SuperAdmin";
    public const string CompanyAdmin = "CompanyAdmin";
    public const string Manager = "Manager";
    public const string Employee = "Employee";

    /// <summary>Roles scoped to a tenant (everything except the platform-level SuperAdmin).</summary>
    public static readonly string[] TenantRoles = { CompanyAdmin, Manager, Employee };

    public static readonly string[] All = { SuperAdmin, CompanyAdmin, Manager, Employee };
}

/// <summary>Authorization policy names used by [Authorize(Policy = ...)].</summary>
public static class AppPolicies
{
    public const string RequireSuperAdmin = "RequireSuperAdmin";
    public const string RequireCompanyAdmin = "RequireCompanyAdmin";
    public const string RequireManager = "RequireManager";
}

/// <summary>Custom JWT claim types.</summary>
public static class AppClaims
{
    public const string TenantId = "tenant_id";
    public const string FullName = "full_name";
}
