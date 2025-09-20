using Microsoft.EntityFrameworkCore;
using NexaFlow.Application.Common.Exceptions;
using NexaFlow.Application.Common.Interfaces;
using NexaFlow.Application.DTOs;
using NexaFlow.Core.Entities;
using NexaFlow.Core.Enums;
using NexaFlow.Infrastructure.Persistence;

namespace NexaFlow.Infrastructure.Services;

public class LeadService(AppDbContext db, ICurrentUser currentUser) : ILeadService
{
    public async Task<IReadOnlyList<LeadDto>> GetAllAsync(CancellationToken ct = default)
    {
        var rows = await ProjectRows(db.Leads.OrderByDescending(l => l.CreatedAt)).ToListAsync(ct);
        return rows.Select(ToDto).ToList();
    }

    public async Task<LeadDto> CreateAsync(CreateLeadDto request, CancellationToken ct = default)
    {
        var tenantId = currentUser.TenantId
                       ?? throw new UnauthorizedAppException("No tenant in the current context.");

        var customerExists = await db.Customers.AnyAsync(c => c.Id == request.CustomerId, ct);
        if (!customerExists)
            throw new NotFoundException("Customer not found.");

        var lead = new Lead
        {
            TenantId = tenantId,
            Title = request.Title,
            Value = request.Value,
            CustomerId = request.CustomerId,
            AssignedToId = request.AssignedToId,
            ExpectedCloseDate = request.ExpectedCloseDate,
            Stage = LeadStage.Prospect
        };

        db.Leads.Add(lead);
        db.Activities.Add(LogActivity(tenantId, lead.CustomerId, $"Lead \"{lead.Title}\" created."));
        await db.SaveChangesAsync(ct);

        return await GetByIdAsync(lead.Id, ct);
    }

    public async Task<LeadDto> UpdateStageAsync(Guid id, UpdateLeadStageDto request, CancellationToken ct = default)
    {
        var lead = await db.Leads.FirstOrDefaultAsync(l => l.Id == id, ct)
                   ?? throw new NotFoundException("Lead not found.");

        var previousStage = lead.Stage;
        lead.Stage = Enum.Parse<LeadStage>(request.Stage);

        // Winning a deal activates the underlying customer.
        if (lead.Stage == LeadStage.Won)
        {
            var customer = await db.Customers.FirstOrDefaultAsync(c => c.Id == lead.CustomerId, ct);
            if (customer is not null)
                customer.Status = CustomerStatus.Active;
        }

        if (lead.Stage != previousStage)
            db.Activities.Add(LogActivity(lead.TenantId, lead.CustomerId,
                $"Lead \"{lead.Title}\" moved from {previousStage} to {lead.Stage}."));

        await db.SaveChangesAsync(ct);

        return await GetByIdAsync(lead.Id, ct);
    }

    /// <summary>Builds a system-generated timeline entry for a pipeline event.</summary>
    private Activity LogActivity(Guid tenantId, Guid customerId, string content) => new()
    {
        TenantId = tenantId,
        CustomerId = customerId,
        Type = ActivityType.StatusChange,
        Content = content,
        CreatedById = currentUser.UserId
    };

    private async Task<LeadDto> GetByIdAsync(Guid id, CancellationToken ct)
    {
        var row = await ProjectRows(db.Leads.Where(l => l.Id == id)).FirstOrDefaultAsync(ct)
                  ?? throw new NotFoundException("Lead not found.");
        return ToDto(row);
    }

    private IQueryable<LeadRow> ProjectRows(IQueryable<Lead> query) =>
        query.Select(l => new LeadRow(
            l.Id, l.Title, l.Value, l.Stage, l.CustomerId,
            l.Customer!.Name,
            db.Users.Where(u => u.Id == l.AssignedToId)
                .Select(u => u.FirstName + " " + u.LastName).FirstOrDefault(),
            l.ExpectedCloseDate));

    private static LeadDto ToDto(LeadRow r) => new(
        r.Id, r.Title, r.Value, r.Stage.ToString(), r.CustomerId, r.CustomerName,
        string.IsNullOrWhiteSpace(r.AssignedToName) ? null : r.AssignedToName.Trim(),
        r.ExpectedCloseDate);

    private sealed record LeadRow(
        Guid Id, string Title, decimal Value, LeadStage Stage, Guid CustomerId,
        string CustomerName, string? AssignedToName, DateTime? ExpectedCloseDate);
}
