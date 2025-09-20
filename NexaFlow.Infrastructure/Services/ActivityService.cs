using Microsoft.EntityFrameworkCore;
using NexaFlow.Application.Common.Exceptions;
using NexaFlow.Application.Common.Interfaces;
using NexaFlow.Application.DTOs;
using NexaFlow.Core.Entities;
using NexaFlow.Core.Enums;
using NexaFlow.Infrastructure.Persistence;

namespace NexaFlow.Infrastructure.Services;

public class ActivityService(AppDbContext db, ICurrentUser currentUser) : IActivityService
{
    public async Task<IReadOnlyList<ActivityDto>> GetForCustomerAsync(Guid customerId, CancellationToken ct = default)
    {
        var customerExists = await db.Customers.AnyAsync(c => c.Id == customerId, ct);
        if (!customerExists)
            throw new NotFoundException("Customer not found.");

        var rows = await ProjectRows(
                db.Activities.Where(a => a.CustomerId == customerId).OrderByDescending(a => a.CreatedAt))
            .ToListAsync(ct);
        return rows.Select(ToDto).ToList();
    }

    public async Task<ActivityDto> CreateAsync(Guid customerId, CreateActivityDto request, CancellationToken ct = default)
    {
        var tenantId = currentUser.TenantId
                       ?? throw new UnauthorizedAppException("No tenant in the current context.");

        var customerExists = await db.Customers.AnyAsync(c => c.Id == customerId, ct);
        if (!customerExists)
            throw new NotFoundException("Customer not found.");

        var activity = new Activity
        {
            TenantId = tenantId,
            CustomerId = customerId,
            Type = Enum.Parse<ActivityType>(request.Type),
            Content = request.Content,
            CreatedById = currentUser.UserId
        };

        db.Activities.Add(activity);
        await db.SaveChangesAsync(ct);

        var row = await ProjectRows(db.Activities.Where(a => a.Id == activity.Id)).FirstAsync(ct);
        return ToDto(row);
    }

    private IQueryable<ActivityRow> ProjectRows(IQueryable<Activity> query) =>
        query.Select(a => new ActivityRow(
            a.Id, a.CustomerId, a.Type, a.Content,
            db.Users.Where(u => u.Id == a.CreatedById)
                .Select(u => u.FirstName + " " + u.LastName).FirstOrDefault(),
            a.CreatedAt));

    private static ActivityDto ToDto(ActivityRow r) => new(
        r.Id, r.CustomerId, r.Type.ToString(), r.Content,
        string.IsNullOrWhiteSpace(r.CreatedByName) ? null : r.CreatedByName.Trim(),
        r.CreatedAt);

    private sealed record ActivityRow(
        Guid Id, Guid CustomerId, ActivityType Type, string Content,
        string? CreatedByName, DateTime CreatedAt);
}
