using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Mvc;
using NexaFlow.Application.Common.Interfaces;

namespace NexaFlow.API.Endpoints;

public static class BackupEndpoints
{
    public static void MapBackupEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/backup").RequireAuthorization();

        group.MapPost("/create", async (ITenantArchiveService archiveService, ICurrentUser currentUser) =>
        {
            var tenantId = currentUser.TenantId ?? throw new UnauthorizedAccessException("Tenant is required.");
            var fileUrl = await archiveService.CreateArchiveAsync(tenantId);
            
            return Results.Ok(new { FileUrl = fileUrl, Message = "Backup created successfully." });
        });

        group.MapPost("/restore", async (HttpRequest request, ITenantArchiveService archiveService, ICurrentUser currentUser, CancellationToken ct) =>
        {
            if (!request.HasFormContentType || !request.Form.Files.Any())
                return Results.BadRequest("No file uploaded.");

            var tenantId = currentUser.TenantId ?? throw new UnauthorizedAccessException("Tenant is required.");
            var file = request.Form.Files[0];

            await using var stream = file.OpenReadStream();
            await archiveService.RestoreArchiveAsync(tenantId, stream, ct);

            return Results.Ok(new { Message = "Restore completed successfully." });
        }).DisableAntiforgery(); // Ensure upload works with minimal APIs
    }
}
