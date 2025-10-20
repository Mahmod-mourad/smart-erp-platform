using NexaFlow.API.Infrastructure;
using NexaFlow.Application.Common.Interfaces;
using NexaFlow.Application.DTOs;
using NexaFlow.Core.Constants;

namespace NexaFlow.API.Endpoints;

public static class AutomationEndpoints
{
    public static IEndpointRouteBuilder MapAutomationEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/automation").WithTags("Automation")
            .RequireAuthorization();

        group.MapGet("/rules", async (IAutomationService svc) =>
            Results.Ok(await svc.GetRulesAsync()))
            .WithSummary("List automation rules for the current tenant.");

        group.MapGet("/rules/{id:guid}", async (Guid id, IAutomationService svc) =>
            Results.Ok(await svc.GetRuleAsync(id)))
            .WithSummary("Get a single automation rule.");

        group.MapPost("/rules", async (CreateWorkflowRuleDto req, IAutomationService svc) =>
            Results.Ok(await svc.CreateAsync(req)))
            .AddEndpointFilter<ValidationFilter<CreateWorkflowRuleDto>>()
            .RequireAuthorization(AppPolicies.RequireManager)
            .WithSummary("Create an automation rule (Manager+).");

        group.MapPut("/rules/{id:guid}", async (Guid id, UpdateWorkflowRuleDto req, IAutomationService svc) =>
            Results.Ok(await svc.UpdateAsync(id, req)))
            .AddEndpointFilter<ValidationFilter<UpdateWorkflowRuleDto>>()
            .RequireAuthorization(AppPolicies.RequireManager)
            .WithSummary("Update an automation rule (Manager+).");

        group.MapPatch("/rules/{id:guid}/toggle", async (Guid id, IAutomationService svc) =>
            Results.Ok(await svc.ToggleAsync(id)))
            .RequireAuthorization(AppPolicies.RequireManager)
            .WithSummary("Activate/deactivate a rule without deleting it (Manager+).");

        group.MapDelete("/rules/{id:guid}", async (Guid id, IAutomationService svc) =>
        {
            await svc.DeleteAsync(id);
            return Results.NoContent();
        })
            .RequireAuthorization(AppPolicies.RequireCompanyAdmin)
            .WithSummary("Delete an automation rule (Company Admin).");

        group.MapGet("/rules/{id:guid}/logs", async (Guid id, IAutomationService svc, int? page, int? pageSize) =>
            Results.Ok(await svc.GetLogsAsync(id, page ?? 1, pageSize ?? 10)))
            .WithSummary("List a rule's execution logs (paged).");

        group.MapPost("/rules/{id:guid}/test", async (Guid id, IAutomationService svc) =>
        {
            await svc.TestRuleAsync(id);
            return Results.Ok(new { message = "Rule executed — check the logs." });
        })
            .RequireAuthorization(AppPolicies.RequireManager)
            .WithSummary("Run a rule immediately, bypassing the scheduler (Manager+).");

        return app;
    }
}
