using NexaFlow.API.Infrastructure;
using NexaFlow.Application.Common.Interfaces;
using NexaFlow.Application.DTOs;

namespace NexaFlow.API.Endpoints;

public static class LeadEndpoints
{
    public static IEndpointRouteBuilder MapLeadEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/leads").WithTags("Leads")
            .RequireAuthorization();

        group.MapGet("/", async (ILeadService svc) =>
            Results.Ok(await svc.GetAllAsync()))
            .WithSummary("List leads for the current tenant.");

        group.MapPost("/", async (CreateLeadDto req, ILeadService svc) =>
            Results.Ok(await svc.CreateAsync(req)))
            .AddEndpointFilter<ValidationFilter<CreateLeadDto>>()
            .WithSummary("Create a lead.");

        group.MapPatch("/{id:guid}/stage", async (Guid id, UpdateLeadStageDto req, ILeadService svc) =>
            Results.Ok(await svc.UpdateStageAsync(id, req)))
            .AddEndpointFilter<ValidationFilter<UpdateLeadStageDto>>()
            .WithSummary("Move a lead to a different pipeline stage.");

        return app;
    }
}
