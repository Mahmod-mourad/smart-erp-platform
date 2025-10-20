using NexaFlow.API.Infrastructure;
using NexaFlow.Application.Common.Interfaces;
using NexaFlow.Application.DTOs;
using NexaFlow.Core.Constants;

namespace NexaFlow.API.Endpoints;

public static class AttendanceEndpoints
{
    public static IEndpointRouteBuilder MapAttendanceEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/attendance").WithTags("Attendance")
            .RequireAuthorization();

        group.MapPost("/check-in", async (CheckInDto req, IAttendanceService svc) =>
            Results.Ok(await svc.CheckInAsync(req.EmployeeId)))
            .WithSummary("Check an employee in for today.");

        group.MapPost("/check-out", async (CheckOutDto req, IAttendanceService svc) =>
            Results.Ok(await svc.CheckOutAsync(req.AttendanceRecordId)))
            .WithSummary("Check an employee out for an existing attendance record.");

        group.MapGet("/employee/{employeeId:guid}", async (Guid employeeId, int year, int month, IAttendanceService svc) =>
            Results.Ok(await svc.GetEmployeeMonthlyAsync(employeeId, year, month)))
            .WithSummary("List an employee's attendance for a given month.");

        group.MapGet("/summary", async (DateOnly date, IAttendanceService svc) =>
            Results.Ok(await svc.GetDailySummaryAsync(date)))
            .RequireAuthorization(AppPolicies.RequireManager)
            .WithSummary("Daily attendance status counts (Manager+).");

        return app;
    }
}
