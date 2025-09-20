using Microsoft.EntityFrameworkCore;
using NexaFlow.Application.Common.Exceptions;
using NexaFlow.Application.Common.Interfaces;
using NexaFlow.Application.DTOs;
using NexaFlow.Core.Constants;
using NexaFlow.Core.Entities;
using NexaFlow.Core.Enums;
using NexaFlow.Infrastructure.Persistence;
using Stateless;

namespace NexaFlow.Infrastructure.Services;

public class LeaveService(AppDbContext db, ICurrentUser currentUser, IWebhookDispatcher webhookDispatcher) : ILeaveService
{
    public async Task<LeaveRequestDto> CreateAsync(CreateLeaveRequestDto request, CancellationToken ct = default)
    {
        var tenantId = currentUser.TenantId
                       ?? throw new UnauthorizedAppException("No tenant in the current context.");

        // An employee applies for themselves: resolve their Employee record via the user link.
        var employee = await db.Employees.FirstOrDefaultAsync(e => e.UserId == currentUser.UserId, ct)
                       ?? throw new NotFoundException("No employee record is linked to the current user.");

        var leave = new LeaveRequest
        {
            TenantId = tenantId,
            EmployeeId = employee.Id,
            Type = Enum.Parse<LeaveType>(request.Type),
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            Reason = request.Reason,
            Status = LeaveStatus.Pending
        };

        db.LeaveRequests.Add(leave);
        await db.SaveChangesAsync(ct);

        return await GetByIdAsync(leave.Id, ct);
    }

    public async Task<IReadOnlyList<LeaveRequestDto>> GetAllAsync(string? status, CancellationToken ct = default)
    {
        var query = db.LeaveRequests.AsQueryable();

        // Managers (and above) see every request; a plain Employee sees only their own.
        if (!IsManagerOrAbove())
            query = query.Where(l => l.Employee.UserId == currentUser.UserId);

        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<LeaveStatus>(status, out var parsed))
            query = query.Where(l => l.Status == parsed);

        var rows = await ProjectRows(query.OrderByDescending(l => l.CreatedAt)).ToListAsync(ct);
        return rows.Select(ToDto).ToList();
    }

    public async Task<LeaveRequestDto> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var row = await ProjectRows(db.LeaveRequests.Where(l => l.Id == id)).FirstOrDefaultAsync(ct)
                  ?? throw new NotFoundException("Leave request not found.");
        return ToDto(row);
    }

    public async Task<LeaveRequestDto> ReviewLeaveAsync(Guid id, ReviewLeaveDto request, CancellationToken ct = default)
    {
        var tenantId = currentUser.TenantId
                       ?? throw new UnauthorizedAppException("No tenant in the current context.");

        var leave = await db.LeaveRequests.FirstOrDefaultAsync(l => l.Id == id, ct)
                    ?? throw new NotFoundException("Leave request not found.");

        var machine = new StateMachine<LeaveStatus, LeaveTrigger>(() => leave.Status, s => leave.Status = s);
        
        machine.Configure(LeaveStatus.Pending)
            .Permit(LeaveTrigger.Approve, LeaveStatus.Approved)
            .Permit(LeaveTrigger.Reject, LeaveStatus.Rejected)
            .Permit(LeaveTrigger.Cancel, LeaveStatus.Cancelled);

        var trigger = request.Approved ? LeaveTrigger.Approve : LeaveTrigger.Reject;
        
        if (!machine.CanFire(trigger))
            throw new ConflictException($"Cannot transition leave request from {leave.Status} via {trigger}.");

        leave.ReviewNote = request.ReviewNote;
        leave.ReviewedById = currentUser.UserId;
        leave.ReviewedAt = DateTime.UtcNow;

        machine.Fire(trigger);

        if (leave.Status == LeaveStatus.Rejected)
        {
            leave.Status = LeaveStatus.Rejected;
            await db.SaveChangesAsync(ct);
            
            await webhookDispatcher.EnqueueWebhookAsync(tenantId, "Leave.Rejected", new { LeaveId = leave.Id, EmployeeId = leave.EmployeeId }, ct);

            return await GetByIdAsync(leave.Id, ct);
        }

        // Mark every working day of the approved range as OnLeave (Fridays are the weekend in Egypt).
        for (var day = leave.StartDate; day <= leave.EndDate; day = day.AddDays(1))
        {
            if (day.DayOfWeek == DayOfWeek.Friday)
                continue;

            db.AttendanceRecords.Add(new AttendanceRecord
            {
                TenantId = tenantId,
                EmployeeId = leave.EmployeeId,
                Date = day,
                Status = AttendanceStatus.OnLeave
            });
        }

        await db.SaveChangesAsync(ct);
        
        await webhookDispatcher.EnqueueWebhookAsync(tenantId, "Leave.Approved", new { LeaveId = leave.Id, EmployeeId = leave.EmployeeId, TotalDays = leave.TotalDays }, ct);
        
        return await GetByIdAsync(leave.Id, ct);
    }

    private bool IsManagerOrAbove() =>
        currentUser.IsInRole(AppRoles.Manager)
        || currentUser.IsInRole(AppRoles.CompanyAdmin)
        || currentUser.IsInRole(AppRoles.SuperAdmin);

    private IQueryable<LeaveRow> ProjectRows(IQueryable<LeaveRequest> query) =>
        query.Select(l => new LeaveRow(
            l.Id, l.EmployeeId,
            l.Employee.FirstName + " " + l.Employee.LastName,
            l.Type, l.StartDate, l.EndDate, l.Reason, l.Status, l.ReviewNote,
            db.Users.Where(u => u.Id == l.ReviewedById)
                .Select(u => u.FirstName + " " + u.LastName).FirstOrDefault()));

    private static LeaveRequestDto ToDto(LeaveRow r) => new(
        r.Id, r.EmployeeId, r.EmployeeName.Trim(), r.Type.ToString(),
        r.StartDate, r.EndDate, r.Reason, r.Status.ToString(),
        (r.EndDate.DayNumber - r.StartDate.DayNumber) + 1,
        r.ReviewNote,
        string.IsNullOrWhiteSpace(r.ReviewedByName) ? null : r.ReviewedByName.Trim());

    private sealed record LeaveRow(
        Guid Id, Guid EmployeeId, string EmployeeName, LeaveType Type,
        DateOnly StartDate, DateOnly EndDate, string Reason, LeaveStatus Status,
        string? ReviewNote, string? ReviewedByName);
}
