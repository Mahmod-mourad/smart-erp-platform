using NexaFlow.API.Infrastructure;
using NexaFlow.Application.Common.Interfaces;
using NexaFlow.Application.DTOs;
using NexaFlow.Core.Constants;
using NexaFlow.Core.Enums;

namespace NexaFlow.API.Endpoints;

public static class IntegrationEndpoints
{
    public static IEndpointRouteBuilder MapIntegrationEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/integrations").WithTags("Integrations")
            .RequireAuthorization(AppPolicies.RequireCompanyAdmin);

        group.MapGet("/", async (IIntegrationService svc) =>
            Results.Ok(await svc.GetAllAsync()))
            .WithSummary("List the tenant's integrations and their status (no credentials returned).");

        group.MapPut("/{type}", async (string type, UpsertIntegrationDto req, IIntegrationService svc) =>
            TryParse(type, out var parsed)
                ? Results.Ok(await svc.UpsertAsync(parsed, req))
                : Results.BadRequest(new { message = UnknownTypeMessage }))
            .AddEndpointFilter<ValidationFilter<UpsertIntegrationDto>>()
            .WithSummary("Save/enable an integration's credentials (Company Admin). Secrets are encrypted at rest.");

        group.MapPost("/{type}/test", async (string type, IIntegrationService svc) =>
            TryParse(type, out var parsed)
                ? Results.Ok(await svc.TestAsync(parsed))
                : Results.BadRequest(new { message = UnknownTypeMessage }))
            .WithSummary("Send a test message through the integration and record the outcome.");

        return app;
    }

    private static bool TryParse(string type, out IntegrationType parsed) =>
        Enum.TryParse(type, ignoreCase: true, out parsed) && Enum.IsDefined(parsed);

    private static readonly string UnknownTypeMessage =
        $"Unknown integration type. Expected one of: {string.Join(", ", Enum.GetNames<IntegrationType>())}.";
}
