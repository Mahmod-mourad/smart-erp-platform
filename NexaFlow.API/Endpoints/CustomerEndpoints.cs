using NexaFlow.API.Infrastructure;
using NexaFlow.Application.Common.Interfaces;
using NexaFlow.Application.DTOs;

namespace NexaFlow.API.Endpoints;

public static class CustomerEndpoints
{
    public static IEndpointRouteBuilder MapCustomerEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/customers").WithTags("Customers")
            .RequireAuthorization();

        group.MapGet("/", async (ICustomerService svc) =>
            Results.Ok(await svc.GetAllAsync()))
            .WithSummary("List customers for the current tenant.");

        group.MapGet("/{id:guid}", async (Guid id, ICustomerService svc) =>
            Results.Ok(await svc.GetByIdAsync(id)))
            .WithSummary("Get a single customer by id.");

        group.MapPost("/", async (CreateCustomerDto req, ICustomerService svc) =>
            Results.Ok(await svc.CreateAsync(req)))
            .AddEndpointFilter<ValidationFilter<CreateCustomerDto>>()
            .WithSummary("Create a customer.");

        group.MapPut("/{id:guid}", async (Guid id, UpdateCustomerDto req, ICustomerService svc) =>
            Results.Ok(await svc.UpdateAsync(id, req)))
            .AddEndpointFilter<ValidationFilter<UpdateCustomerDto>>()
            .WithSummary("Update a customer.");

        group.MapDelete("/{id:guid}", async (Guid id, ICustomerService svc) =>
        {
            await svc.DeleteAsync(id);
            return Results.NoContent();
        })
            .WithSummary("Delete a customer.");

        group.MapGet("/{id:guid}/activities", async (Guid id, IActivityService svc) =>
            Results.Ok(await svc.GetForCustomerAsync(id)))
            .WithSummary("List a customer's activity timeline (newest first).");

        group.MapPost("/{id:guid}/activities", async (Guid id, CreateActivityDto req, IActivityService svc) =>
            Results.Ok(await svc.CreateAsync(id, req)))
            .AddEndpointFilter<ValidationFilter<CreateActivityDto>>()
            .WithSummary("Log an activity (note/call/email/meeting) on a customer.");

        return app;
    }
}
