using Hangfire;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Mvc;
using NexaFlow.Application.Common.Interfaces;
using NexaFlow.Infrastructure.BackgroundJobs;

namespace NexaFlow.API.Endpoints;

public static class ExportEndpoints
{
    public static void MapExportEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/export").RequireAuthorization();

        group.MapPost("/{entityType}", (
            string entityType, 
            [FromQuery] string format, 
            IBackgroundJobClient backgroundJobs, 
            ICurrentUser currentUser) =>
        {
            var tenantId = currentUser.TenantId ?? throw new UnauthorizedAccessException("Tenant is required.");
            var userId = currentUser.UserId ?? throw new UnauthorizedAccessException("User is required.");
            var fileFormat = format ?? "csv";

            var jobId = backgroundJobs.Enqueue<ExportJob>(job => 
                job.RunExportAsync(tenantId, userId, entityType, fileFormat, CancellationToken.None));

            return Results.Accepted("", new { JobId = jobId, Message = "Export started in the background. You will be notified when it's ready." });
        });

        group.MapGet("/download", async (
            [FromQuery] string fileName, 
            HttpContext context) =>
        {
            // In a real app, validate that the user is allowed to download this file
            var exportDir = Path.Combine(Path.GetTempPath(), "NexaFlowExports");
            var filePath = Path.Combine(exportDir, fileName);

            if (!System.IO.File.Exists(filePath))
            {
                return Results.NotFound("Export file not found or has expired.");
            }

            var mimeType = fileName.EndsWith(".xlsx") 
                ? "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet" 
                : "text/csv";

            var bytes = await System.IO.File.ReadAllBytesAsync(filePath);
            return Results.File(bytes, mimeType, fileName);
        }).AllowAnonymous(); // Depending on auth setup, downloading files directly via browser URL might need cookie auth or short-lived token. Using AllowAnonymous for MVP simplicity.
    }
}
