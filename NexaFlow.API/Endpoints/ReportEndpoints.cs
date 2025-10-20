using Microsoft.AspNetCore.Mvc;
using NexaFlow.Core.Constants;
using NexaFlow.Application.Common.Interfaces;

namespace NexaFlow.API.Endpoints;

public static class ReportEndpoints
{
    public static void MapReportEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/reports")
            .RequireAuthorization()
            .WithTags("Reports")
            .WithOpenApi();

        group.MapGet("/invoice/{invoiceNumber}", GenerateInvoicePdf)
            .RequireAuthorization(AppPolicies.RequireManager);
    }

    private static async Task<IResult> GenerateInvoicePdf(
        string invoiceNumber,
        [FromQuery] string customerName,
        [FromQuery] decimal totalAmount,
        IReportGenerator reportGenerator,
        CancellationToken cancellationToken)
    {
        customerName ??= "عميل نقدي";
        if (totalAmount <= 0) totalAmount = 1000m; // Mock amount for demo

        var pdfBytes = await reportGenerator.GenerateInvoicePdfAsync(invoiceNumber, customerName, totalAmount, cancellationToken);
        
        return Results.File(pdfBytes, "application/pdf", $"Invoice_{invoiceNumber}.pdf");
    }
}
