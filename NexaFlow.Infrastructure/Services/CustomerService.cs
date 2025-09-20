using Microsoft.EntityFrameworkCore;
using NexaFlow.Application.Common.Exceptions;
using NexaFlow.Application.Common.Interfaces;
using NexaFlow.Application.DTOs;
using NexaFlow.Core.Entities;
using NexaFlow.Core.Enums;
using NexaFlow.Infrastructure.Persistence;

namespace NexaFlow.Infrastructure.Services;

public class CustomerService(AppDbContext db, ICurrentUser currentUser) : ICustomerService
{
    public async Task<IReadOnlyList<CustomerDto>> GetAllAsync(CancellationToken ct = default)
    {
        var rows = await ProjectRows(db.Customers.OrderByDescending(c => c.CreatedAt)).ToListAsync(ct);
        return rows.Select(ToDto).ToList();
    }

    public async Task<CustomerDto> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var row = await ProjectRows(db.Customers.Where(c => c.Id == id)).FirstOrDefaultAsync(ct)
                  ?? throw new NotFoundException("Customer not found.");
        return ToDto(row);
    }

    public async Task<CustomerDto> CreateAsync(CreateCustomerDto request, CancellationToken ct = default)
    {
        var tenantId = currentUser.TenantId
                       ?? throw new UnauthorizedAppException("No tenant in the current context.");

        var customer = new Customer
        {
            TenantId = tenantId,
            Name = request.Name,
            Email = request.Email,
            Phone = request.Phone,
            Company = request.Company,
            Notes = request.Notes,
            AssignedToId = request.AssignedToId,
            Status = CustomerStatus.Active
        };

        db.Customers.Add(customer);
        db.Activities.Add(new Activity
        {
            TenantId = tenantId,
            CustomerId = customer.Id,
            Type = ActivityType.StatusChange,
            Content = "Customer created.",
            CreatedById = currentUser.UserId
        });
        await db.SaveChangesAsync(ct);

        return await GetByIdAsync(customer.Id, ct);
    }

    public async Task<CustomerDto> UpdateAsync(Guid id, UpdateCustomerDto request, CancellationToken ct = default)
    {
        var customer = await db.Customers.FirstOrDefaultAsync(c => c.Id == id, ct)
                       ?? throw new NotFoundException("Customer not found.");

        customer.Name = request.Name;
        customer.Email = request.Email;
        customer.Phone = request.Phone;
        customer.Company = request.Company;
        customer.Notes = request.Notes;
        customer.AssignedToId = request.AssignedToId;
        customer.Status = Enum.Parse<CustomerStatus>(request.Status);

        await db.SaveChangesAsync(ct);

        return await GetByIdAsync(customer.Id, ct);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var customer = await db.Customers.FirstOrDefaultAsync(c => c.Id == id, ct)
                       ?? throw new NotFoundException("Customer not found.");
        db.Customers.Remove(customer);
        await db.SaveChangesAsync(ct);
    }

    // SQL-translatable projection. AssignedToName comes from a correlated lookup into the
    // Users table (which carries the tenant query filter, keeping it tenant-scoped). The
    // enum is kept as its value here and turned into its name in memory below.
    private IQueryable<CustomerRow> ProjectRows(IQueryable<Customer> query) =>
        query.Select(c => new CustomerRow(
            c.Id, c.Name, c.Email, c.Phone, c.Company, c.Status,
            db.Users.Where(u => u.Id == c.AssignedToId)
                .Select(u => u.FirstName + " " + u.LastName).FirstOrDefault(),
            c.Leads.Count,
            c.CreatedAt));

    private static CustomerDto ToDto(CustomerRow r) => new(
        r.Id, r.Name, r.Email, r.Phone, r.Company,
        r.Status.ToString(),
        string.IsNullOrWhiteSpace(r.AssignedToName) ? null : r.AssignedToName.Trim(),
        r.LeadsCount,
        r.CreatedAt);

    private sealed record CustomerRow(
        Guid Id, string Name, string? Email, string? Phone, string? Company,
        CustomerStatus Status, string? AssignedToName, int LeadsCount, DateTime CreatedAt);
}
