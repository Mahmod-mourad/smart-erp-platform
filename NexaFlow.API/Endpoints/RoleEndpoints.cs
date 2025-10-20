using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using NexaFlow.Application.Common.Interfaces;
using NexaFlow.Application.Common.Security;
using NexaFlow.Application.DTOs;

namespace NexaFlow.API.Endpoints;

public static class RoleEndpoints
{
    public static void MapRoleEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/roles")
            .RequireAuthorization(AppPermissions.Settings.ManageRoles)
            .WithTags("Roles");

        group.MapGet("/", async (IRoleService service, CancellationToken ct) =>
            Results.Ok(await service.GetAllAsync(ct)));

        group.MapGet("/{id:guid}", async (Guid id, IRoleService service, CancellationToken ct) =>
            Results.Ok(await service.GetByIdAsync(id, ct)));

        group.MapPost("/", async (CreateRoleDto request, IValidator<CreateRoleDto> validator, IRoleService service, CancellationToken ct) =>
        {
            var val = await validator.ValidateAsync(request, ct);
            if (!val.IsValid) return Results.ValidationProblem(val.ToDictionary());

            var role = await service.CreateAsync(request, ct);
            return Results.Created($"/api/roles/{role.Id}", role);
        });

        group.MapPut("/{id:guid}", async (Guid id, UpdateRoleDto request, IValidator<UpdateRoleDto> validator, IRoleService service, CancellationToken ct) =>
        {
            var val = await validator.ValidateAsync(request, ct);
            if (!val.IsValid) return Results.ValidationProblem(val.ToDictionary());

            var role = await service.UpdateAsync(id, request, ct);
            return Results.Ok(role);
        });

        group.MapDelete("/{id:guid}", async (Guid id, IRoleService service, CancellationToken ct) =>
        {
            await service.DeleteAsync(id, ct);
            return Results.NoContent();
        });

        group.MapPost("/assign", async (AssignRoleRequest request, IRoleService service, CancellationToken ct) =>
        {
            await service.AssignRoleToUserAsync(request.UserId, request.RoleId, ct);
            return Results.Ok();
        });
        
        // Helper endpoint to list all available system permissions so UI can show checkboxes
        app.MapGet("/api/permissions", () => Results.Ok(AppPermissions.All))
            .RequireAuthorization()
            .WithTags("Roles");
    }
}

public record AssignRoleRequest(Guid UserId, Guid? RoleId);
