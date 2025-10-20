using NexaFlow.Application.Common.Interfaces;
using NexaFlow.Core.Constants;

namespace NexaFlow.API.Endpoints;

public static class TenantEndpoints
{
    public static IEndpointRouteBuilder MapTenantEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/tenants").WithTags("Tenants").RequireAuthorization();

        group.MapGet("/current", async (ITenantService tenants) =>
            Results.Ok(await tenants.GetCurrentAsync()))
            .WithSummary("Get the caller's own company.");

        group.MapGet("/{id:guid}", async (Guid id, ITenantService tenants) =>
            Results.Ok(await tenants.GetByIdAsync(id)))
            .RequireAuthorization(AppPolicies.RequireSuperAdmin)
            .WithSummary("Get any tenant (platform admin only).");

        return app;
    }
}
