using NexaFlow.Application.Common.Interfaces;
using NexaFlow.Core.Constants;
using NexaFlow.Infrastructure.PDF;
using QuestPDF.Fluent;

namespace NexaFlow.API.Endpoints;

public static class PayrollEndpoints
{
    public static IEndpointRouteBuilder MapPayrollEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/payroll").WithTags("Payroll")
            .RequireAuthorization(AppPolicies.RequireManager);

        group.MapGet("/{employeeId:guid}", async (Guid employeeId, int year, int month, IPayrollService svc) =>
            Results.Ok(await svc.CalculatePayslipAsync(employeeId, year, month)))
            .WithSummary("Compute a payslip for an employee/month (Manager+).");

        group.MapGet("/{employeeId:guid}/pdf", async (Guid employeeId, int year, int month, IPayrollService svc) =>
        {
            var payslip = await svc.CalculatePayslipAsync(employeeId, year, month);
            var bytes = new PayslipDocument(payslip).GeneratePdf();
            var fileName = $"payslip_{payslip.EmployeeFullName.Replace(' ', '_')}_{year}_{month:D2}.pdf";
            return Results.File(bytes, "application/pdf", fileName);
        })
            .WithSummary("Download a payslip PDF (Manager+).");

        return app;
    }
}
