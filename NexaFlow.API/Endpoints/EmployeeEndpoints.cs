using NexaFlow.API.Infrastructure;
using NexaFlow.Application.Common.Interfaces;
using NexaFlow.Application.DTOs;
using NexaFlow.Core.Constants;

namespace NexaFlow.API.Endpoints;

public static class EmployeeEndpoints
{
    public static IEndpointRouteBuilder MapEmployeeEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/employees").WithTags("Employees")
            .RequireAuthorization();

        group.MapGet("/", async (IEmployeeService svc) =>
            Results.Ok(await svc.GetAllAsync()))
            .WithSummary("List employees for the current tenant.");

        group.MapGet("/{id:guid}", async (Guid id, IEmployeeService svc) =>
            Results.Ok(await svc.GetByIdAsync(id)))
            .WithSummary("Get a single employee by id.");

        group.MapPost("/", async (CreateEmployeeDto req, IEmployeeService svc) =>
            Results.Ok(await svc.CreateAsync(req)))
            .AddEndpointFilter<ValidationFilter<CreateEmployeeDto>>()
            .RequireAuthorization(AppPolicies.RequireManager)
            .WithSummary("Create an employee.");

        group.MapPut("/{id:guid}", async (Guid id, UpdateEmployeeDto req, IEmployeeService svc) =>
            Results.Ok(await svc.UpdateAsync(id, req)))
            .AddEndpointFilter<ValidationFilter<UpdateEmployeeDto>>()
            .RequireAuthorization(AppPolicies.RequireManager)
            .WithSummary("Update an employee.");

        group.MapDelete("/{id:guid}", async (Guid id, IEmployeeService svc) =>
        {
            await svc.DeleteAsync(id);
            return Results.NoContent();
        })
            .RequireAuthorization(AppPolicies.RequireCompanyAdmin)
            .WithSummary("Delete an employee.");

        return app;
    }
}
