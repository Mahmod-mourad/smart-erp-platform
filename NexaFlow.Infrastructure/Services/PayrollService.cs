using Microsoft.EntityFrameworkCore;
using NexaFlow.Application.Common.Exceptions;
using NexaFlow.Application.Common.Interfaces;
using NexaFlow.Application.DTOs;
using NexaFlow.Core.Enums;
using NexaFlow.Infrastructure.Persistence;

namespace NexaFlow.Infrastructure.Services;

public class PayrollService(AppDbContext db) : IPayrollService
{
    public async Task<PayslipDto> CalculatePayslipAsync(
        Guid employeeId, int year, int month, CancellationToken ct = default)
    {
        var employee = await db.Employees.FirstOrDefaultAsync(e => e.Id == employeeId, ct)
                       ?? throw new NotFoundException("Employee not found.");

        var start = new DateOnly(year, month, 1);
        var end = start.AddMonths(1);

        var statuses = await db.AttendanceRecords
            .Where(a => a.EmployeeId == employeeId && a.Date >= start && a.Date < end)
            .Select(a => a.Status)
            .ToListAsync(ct);

        var workingDays = CountWorkingDays(year, month);
        var presentDays = statuses.Count(s => s is AttendanceStatus.Present or AttendanceStatus.Late);
        var leaveDays = statuses.Count(s => s == AttendanceStatus.OnLeave);
        var absentDays = Math.Max(0, workingDays - presentDays - leaveDays);

        var grossSalary = employee.BaseSalary + employee.Allowances;
        var dailyRate = workingDays > 0 ? employee.BaseSalary / workingDays : 0m;
        var deduction = Math.Round(dailyRate * absentDays, 2);
        var netSalary = grossSalary - deduction;

        return new PayslipDto(
            employee.Id,
            employee.FullName,
            employee.Department,
            employee.Position,
            start,
            employee.BaseSalary,
            employee.Allowances,
            grossSalary,
            workingDays,
            presentDays,
            leaveDays,
            absentDays,
            Math.Round(dailyRate, 2),
            deduction,
            netSalary);
    }

    /// <summary>Days in the month that are not Fridays (the Egyptian weekend).</summary>
    private static int CountWorkingDays(int year, int month)
    {
        var days = DateTime.DaysInMonth(year, month);
        var count = 0;
        for (var d = 1; d <= days; d++)
        {
            if (new DateOnly(year, month, d).DayOfWeek != DayOfWeek.Friday)
                count++;
        }
        return count;
    }
}
