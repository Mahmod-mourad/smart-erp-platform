using NexaFlow.API.Infrastructure;
using NexaFlow.Application.Common.Interfaces;
using NexaFlow.Application.DTOs;
using NexaFlow.Core.Constants;

namespace NexaFlow.API.Endpoints;

public static class InvitationEndpoints
{
    public static IEndpointRouteBuilder MapInvitationEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/invitations").WithTags("Invitations")
            .RequireAuthorization(AppPolicies.RequireCompanyAdmin);

        group.MapPost("/", async (InviteMemberRequest req, IInvitationService svc) =>
            Results.Ok(await svc.InviteAsync(req)))
            .AddEndpointFilter<ValidationFilter<InviteMemberRequest>>()
            .WithSummary("Invite a team member by email (T-015).");

        group.MapGet("/", async (IInvitationService svc) =>
            Results.Ok(await svc.GetPendingAsync()))
            .WithSummary("List pending invitations for the current tenant.");

        group.MapDelete("/{id:guid}", async (Guid id, IInvitationService svc) =>
        {
            await svc.RevokeAsync(id);
            return Results.NoContent();
        })
            .WithSummary("Revoke a pending invitation.");

        return app;
    }
}
