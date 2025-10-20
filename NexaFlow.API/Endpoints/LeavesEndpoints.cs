using NexaFlow.API.Infrastructure;
using NexaFlow.Application.Common.Interfaces;
using NexaFlow.Application.DTOs;
using NexaFlow.Core.Constants;

namespace NexaFlow.API.Endpoints;

public static class LeavesEndpoints
{
    public static IEndpointRouteBuilder MapLeavesEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/leaves").WithTags("Leaves")
            .RequireAuthorization();

        group.MapPost("/", async (CreateLeaveRequestDto req, ILeaveService svc) =>
            Results.Ok(await svc.CreateAsync(req)))
            .AddEndpointFilter<ValidationFilter<CreateLeaveRequestDto>>()
            .WithSummary("Apply for leave (for the current user's employee record).");

        group.MapGet("/", async (string? status, ILeaveService svc) =>
            Results.Ok(await svc.GetAllAsync(status)))
            .WithSummary("List leave requests (Manager+ sees all; an employee sees their own).");

        group.MapGet("/{id:guid}", async (Guid id, ILeaveService svc) =>
            Results.Ok(await svc.GetByIdAsync(id)))
            .WithSummary("Get a single leave request by id.");

        group.MapPatch("/{id:guid}/review", async (Guid id, ReviewLeaveDto req, ILeaveService svc) =>
            Results.Ok(await svc.ReviewLeaveAsync(id, req)))
            .AddEndpointFilter<ValidationFilter<ReviewLeaveDto>>()
            .RequireAuthorization(AppPolicies.RequireManager)
            .WithSummary("Approve or reject a leave request (Manager+).");

        return app;
    }
}
