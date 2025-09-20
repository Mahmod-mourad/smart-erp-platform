using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NexaFlow.Core.Constants;
using NexaFlow.Infrastructure.Identity;

namespace NexaFlow.Infrastructure.Persistence;

public static class DbSeeder
{
    /// <summary>Ensures the canonical roles exist. Idempotent — safe to run on every startup.</summary>
    public static async Task SeedRolesAsync(IServiceProvider services)
    {
        var roleManager = services.GetRequiredService<RoleManager<ApplicationRole>>();
        var logger = services.GetRequiredService<ILogger<RoleSeederLog>>();

        foreach (var roleName in AppRoles.All)
        {
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                await roleManager.CreateAsync(new ApplicationRole(roleName) { TenantId = null });
                logger.LogInformation("Seeded role {Role}", roleName);
            }
        }
    }

    /// <summary>Marker type for a typed logger category.</summary>
    public sealed class RoleSeederLog;
}
