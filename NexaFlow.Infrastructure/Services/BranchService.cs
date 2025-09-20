using Microsoft.EntityFrameworkCore;
using NexaFlow.Application.Common.Exceptions;
using NexaFlow.Application.Common.Interfaces;
using NexaFlow.Application.DTOs;
using NexaFlow.Core.Entities;
using NexaFlow.Infrastructure.Persistence;

namespace NexaFlow.Infrastructure.Services;

public class BranchService(AppDbContext db, ICurrentUser currentUser) : IBranchService
{
    public async Task<BranchDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var branch = await db.Branches.FirstOrDefaultAsync(b => b.Id == id, cancellationToken)
            ?? throw new NotFoundException("Branch not found.");
        return ToDto(branch);
    }

    public async Task<IReadOnlyList<BranchDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var branches = await db.Branches.OrderBy(b => b.Name).ToListAsync(cancellationToken);
        return branches.Select(ToDto).ToList();
    }

    public async Task<BranchDto> CreateAsync(CreateBranchDto dto, CancellationToken cancellationToken = default)
    {
        var tenantId = currentUser.TenantId
                       ?? throw new UnauthorizedAppException("No tenant in the current context.");

        var branch = new Branch
        {
            TenantId = tenantId,
            Name = dto.Name,
            Address = dto.Address,
            City = dto.City,
            Phone = dto.Phone,
            IsHeadquarters = dto.IsHeadquarters
        };

        db.Branches.Add(branch);
        await db.SaveChangesAsync(cancellationToken);

        return ToDto(branch);
    }

    public async Task<BranchDto> UpdateAsync(Guid id, UpdateBranchDto dto, CancellationToken cancellationToken = default)
    {
        var branch = await db.Branches.FirstOrDefaultAsync(b => b.Id == id, cancellationToken)
            ?? throw new NotFoundException("Branch not found.");

        branch.Name = dto.Name;
        branch.Address = dto.Address;
        branch.City = dto.City;
        branch.Phone = dto.Phone;
        branch.IsHeadquarters = dto.IsHeadquarters;

        await db.SaveChangesAsync(cancellationToken);

        return ToDto(branch);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var branch = await db.Branches.FirstOrDefaultAsync(b => b.Id == id, cancellationToken)
            ?? throw new NotFoundException("Branch not found.");
            
        db.Branches.Remove(branch);
        await db.SaveChangesAsync(cancellationToken);
    }

    private static BranchDto ToDto(Branch b) => new(
        b.Id, b.Name, b.Address, b.City, b.Phone, b.IsHeadquarters);
}
