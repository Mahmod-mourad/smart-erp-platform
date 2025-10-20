using Microsoft.AspNetCore.Authorization;
using NexaFlow.Core.Constants;

namespace NexaFlow.API.Infrastructure.Authorization;

public class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
{
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, PermissionRequirement requirement)
    {
        // SuperAdmin always has all permissions
        if (context.User.IsInRole(AppRoles.SuperAdmin))
        {
            context.Succeed(requirement);
            return Task.CompletedTask;
        }

        // Check if the user has the specific permission claim
        var hasPermission = context.User.Claims.Any(c => c.Type == "Permission" && c.Value == requirement.Permission);
        
        // Also fallback to CompanyAdmin having implicit all-access for now
        if (hasPermission || context.User.IsInRole(AppRoles.CompanyAdmin))
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}
