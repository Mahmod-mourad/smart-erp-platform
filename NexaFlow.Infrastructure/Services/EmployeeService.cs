using Microsoft.EntityFrameworkCore;
using NexaFlow.Application.Common.Exceptions;
using NexaFlow.Application.Common.Interfaces;
using NexaFlow.Application.DTOs;
using NexaFlow.Core.Entities;
using NexaFlow.Core.Enums;
using NexaFlow.Infrastructure.Persistence;

namespace NexaFlow.Infrastructure.Services;

public class EmployeeService(AppDbContext db, ICurrentUser currentUser) : IEmployeeService
{
    public async Task<IReadOnlyList<EmployeeDto>> GetAllAsync(CancellationToken ct = default)
    {
        var rows = await db.Employees.OrderBy(e => e.FirstName).ThenBy(e => e.LastName).ToListAsync(ct);
        return rows.Select(ToDto).ToList();
    }

    public async Task<EmployeeDto> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var employee = await db.Employees.FirstOrDefaultAsync(e => e.Id == id, ct)
                       ?? throw new NotFoundException("Employee not found.");
        return ToDto(employee);
    }

    public async Task<EmployeeDto> CreateAsync(CreateEmployeeDto request, CancellationToken ct = default)
    {
        var tenantId = currentUser.TenantId
                       ?? throw new UnauthorizedAppException("No tenant in the current context.");

        var employee = new Employee
        {
            TenantId = tenantId,
            FirstName = request.FirstName,
            LastName = request.LastName,
            Email = request.Email,
            Phone = request.Phone,
            NationalId = request.NationalId,
            Department = request.Department,
            Position = request.Position,
            HireDate = request.HireDate,
            BaseSalary = request.BaseSalary,
            Allowances = request.Allowances,
            BranchId = request.BranchId,
            Status = EmployeeStatus.Active
        };

        db.Employees.Add(employee);
        await db.SaveChangesAsync(ct);

        return ToDto(employee);
    }

    public async Task<EmployeeDto> UpdateAsync(Guid id, UpdateEmployeeDto request, CancellationToken ct = default)
    {
        var employee = await db.Employees.FirstOrDefaultAsync(e => e.Id == id, ct)
                       ?? throw new NotFoundException("Employee not found.");

        employee.FirstName = request.FirstName;
        employee.LastName = request.LastName;
        employee.Email = request.Email;
        employee.Phone = request.Phone;
        employee.Department = request.Department;
        employee.Position = request.Position;
        employee.BaseSalary = request.BaseSalary;
        employee.Allowances = request.Allowances;
        employee.BranchId = request.BranchId;
        employee.Status = Enum.Parse<EmployeeStatus>(request.Status);

        await db.SaveChangesAsync(ct);

        return ToDto(employee);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var employee = await db.Employees.FirstOrDefaultAsync(e => e.Id == id, ct)
                       ?? throw new NotFoundException("Employee not found.");
        db.Employees.Remove(employee);
        await db.SaveChangesAsync(ct);
    }

    private static EmployeeDto ToDto(Employee e) => new(
        e.Id, e.FullName, e.FirstName, e.LastName, e.Email, e.Phone,
        e.Department, e.Position, e.HireDate, e.BaseSalary, e.Allowances,
        e.Status.ToString(), e.BranchId, e.CreatedAt);
}
