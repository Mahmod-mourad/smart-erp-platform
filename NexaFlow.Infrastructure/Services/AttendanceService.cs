using Microsoft.EntityFrameworkCore;
using NexaFlow.Application.Common.Exceptions;
using NexaFlow.Application.Common.Interfaces;
using NexaFlow.Application.DTOs;
using NexaFlow.Core.Entities;
using NexaFlow.Core.Enums;
using NexaFlow.Infrastructure.Persistence;

namespace NexaFlow.Infrastructure.Services;

public class AttendanceService(AppDbContext db, ICurrentUser currentUser) : IAttendanceService
{
    private static readonly TimeOnly LateThreshold = new(9, 0);

    public async Task<AttendanceDto> CheckInAsync(Guid employeeId, CancellationToken ct = default)
    {
        var tenantId = currentUser.TenantId
                       ?? throw new UnauthorizedAppException("No tenant in the current context.");

        var employee = await db.Employees.FirstOrDefaultAsync(e => e.Id == employeeId, ct)
                       ?? throw new NotFoundException("Employee not found.");

        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var alreadyIn = await db.AttendanceRecords
            .AnyAsync(a => a.EmployeeId == employeeId && a.Date == today && a.CheckIn != null, ct);
        if (alreadyIn)
            throw new ConflictException("Already checked in today.");

        var checkInTime = TimeOnly.FromDateTime(DateTime.UtcNow);
        var status = checkInTime > LateThreshold ? AttendanceStatus.Late : AttendanceStatus.Present;

        var record = new AttendanceRecord
        {
            TenantId = tenantId,
            EmployeeId = employeeId,
            Date = today,
            CheckIn = checkInTime,
            Status = status
        };

        db.AttendanceRecords.Add(record);
        await db.SaveChangesAsync(ct);

        return ToDto(record, employee.FullName);
    }

    public async Task<AttendanceDto> CheckOutAsync(Guid attendanceRecordId, CancellationToken ct = default)
    {
        var record = await db.AttendanceRecords
                         .Include(a => a.Employee)
                         .FirstOrDefaultAsync(a => a.Id == attendanceRecordId, ct)
                     ?? throw new NotFoundException("Attendance record not found.");

        if (record.CheckOut.HasValue)
            throw new ConflictException("Already checked out.");

        record.CheckOut = TimeOnly.FromDateTime(DateTime.UtcNow);
        await db.SaveChangesAsync(ct);

        return ToDto(record, record.Employee.FullName);
    }

    public async Task<IReadOnlyList<AttendanceDto>> GetEmployeeMonthlyAsync(
        Guid employeeId, int year, int month, CancellationToken ct = default)
    {
        var start = new DateOnly(year, month, 1);
        var end = start.AddMonths(1);

        var records = await db.AttendanceRecords
            .Include(a => a.Employee)
            .Where(a => a.EmployeeId == employeeId && a.Date >= start && a.Date < end)
            .OrderBy(a => a.Date)
            .ToListAsync(ct);

        return records.Select(r => ToDto(r, r.Employee.FullName)).ToList();
    }

    public async Task<AttendanceSummaryDto> GetDailySummaryAsync(DateOnly date, CancellationToken ct = default)
    {
        var counts = await db.AttendanceRecords
            .Where(a => a.Date == date)
            .GroupBy(a => a.Status)
            .Select(g => new { g.Key, Count = g.Count() })
            .ToListAsync(ct);

        int CountFor(AttendanceStatus s) => counts.FirstOrDefault(c => c.Key == s)?.Count ?? 0;

        return new AttendanceSummaryDto(
            date,
            CountFor(AttendanceStatus.Present),
            CountFor(AttendanceStatus.Absent),
            CountFor(AttendanceStatus.Late),
            CountFor(AttendanceStatus.OnLeave));
    }

    private static AttendanceDto ToDto(AttendanceRecord r, string employeeName) => new(
        r.Id,
        r.EmployeeId,
        employeeName,
        r.Date,
        r.CheckIn?.ToString("HH:mm"),
        r.CheckOut?.ToString("HH:mm"),
        r.Status.ToString(),
        FormatHours(r.WorkingHours));

    private static string? FormatHours(TimeSpan? span) =>
        span is { } s ? $"{(int)s.TotalHours:D2}:{s.Minutes:D2}" : null;
}
