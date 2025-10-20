using NexaFlow.API.Infrastructure;
using NexaFlow.Application.Common.Interfaces;
using NexaFlow.Application.DTOs;
using NexaFlow.Core.Constants;

namespace NexaFlow.API.Endpoints;

public static class BranchEndpoints
{
    public static IEndpointRouteBuilder MapBranchEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/branches").WithTags("Branches")
            .RequireAuthorization();

        group.MapGet("/", async (IBranchService svc) =>
            Results.Ok(await svc.GetAllAsync()))
            .WithSummary("List branches for the current tenant.");

        group.MapGet("/{id:guid}", async (Guid id, IBranchService svc) =>
            Results.Ok(await svc.GetByIdAsync(id)))
            .WithSummary("Get a single branch by id.");

        group.MapPost("/", async (CreateBranchDto req, IBranchService svc) =>
            Results.Ok(await svc.CreateAsync(req)))
            .AddEndpointFilter<ValidationFilter<CreateBranchDto>>()
            .RequireAuthorization(AppPolicies.RequireManager)
            .WithSummary("Create a branch.");

        group.MapPut("/{id:guid}", async (Guid id, UpdateBranchDto req, IBranchService svc) =>
            Results.Ok(await svc.UpdateAsync(id, req)))
            .AddEndpointFilter<ValidationFilter<UpdateBranchDto>>()
            .RequireAuthorization(AppPolicies.RequireManager)
            .WithSummary("Update a branch.");

        group.MapDelete("/{id:guid}", async (Guid id, IBranchService svc) =>
        {
            await svc.DeleteAsync(id);
            return Results.NoContent();
        })
            .RequireAuthorization(AppPolicies.RequireCompanyAdmin)
            .WithSummary("Delete a branch.");

        return app;
    }
}
