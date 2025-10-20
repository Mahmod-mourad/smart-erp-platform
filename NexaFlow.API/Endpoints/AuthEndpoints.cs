using NexaFlow.API.Infrastructure;
using NexaFlow.Application.Common.Interfaces;
using NexaFlow.Application.DTOs;

namespace NexaFlow.API.Endpoints;

public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/auth").WithTags("Auth");

        group.MapPost("/register-company", async (RegisterCompanyRequest req, IAuthService auth, HttpContext ctx) =>
            Results.Ok(await auth.RegisterCompanyAsync(req, ClientIp(ctx))))
            .AddEndpointFilter<ValidationFilter<RegisterCompanyRequest>>()
            .WithSummary("Onboard a new company + its first admin (T-010).")
            .AllowAnonymous();

        group.MapPost("/login", async (LoginRequest req, IAuthService auth, HttpContext ctx) =>
            Results.Ok(await auth.LoginAsync(req, ClientIp(ctx))))
            .AddEndpointFilter<ValidationFilter<LoginRequest>>()
            .WithSummary("Authenticate and receive access + refresh tokens.")
            .AllowAnonymous();

        group.MapPost("/refresh", async (RefreshTokenRequest req, IAuthService auth, HttpContext ctx) =>
            Results.Ok(await auth.RefreshTokenAsync(req.RefreshToken, ClientIp(ctx))))
            .WithSummary("Rotate a refresh token for a new access token.")
            .AllowAnonymous();

        group.MapPost("/accept-invite", async (AcceptInvitationRequest req, IAuthService auth, HttpContext ctx) =>
            Results.Ok(await auth.AcceptInvitationAsync(req, ClientIp(ctx))))
            .AddEndpointFilter<ValidationFilter<AcceptInvitationRequest>>()
            .WithSummary("Complete registration from an invitation link (T-015).")
            .AllowAnonymous();

        group.MapPost("/logout", async (RefreshTokenRequest req, IAuthService auth) =>
        {
            await auth.RevokeRefreshTokenAsync(req.RefreshToken);
            return Results.NoContent();
        })
            .RequireAuthorization()
            .WithSummary("Revoke the current refresh token.");

        return app;
    }

    private static string? ClientIp(HttpContext ctx) => ctx.Connection.RemoteIpAddress?.ToString();
}
