using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using NexaFlow.Application.Common.Interfaces;
using NexaFlow.Application.DTOs;

namespace NexaFlow.API.Endpoints;

public static class UserPreferencesEndpoints
{
    public static void MapUserPreferencesEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/preferences").RequireAuthorization();

        group.MapGet("/", async (IUserPreferencesService service, CancellationToken ct) =>
        {
            var result = await service.GetPreferencesAsync(ct);
            return Results.Ok(result);
        });

        group.MapPut("/", async (UpdateUserPreferencesDto dto, IUserPreferencesService service, CancellationToken ct) =>
        {
            var result = await service.UpdatePreferencesAsync(dto, ct);
            return Results.Ok(result);
        });
    }
}
