using AutoMapper;
using Microsoft.EntityFrameworkCore;
using NexaFlow.Application.Common.Exceptions;
using NexaFlow.Application.Common.Interfaces;
using NexaFlow.Application.DTOs;
using NexaFlow.Infrastructure.Persistence;

namespace NexaFlow.Infrastructure.Services;

public class TenantService(AppDbContext db, ICurrentUser currentUser, IMapper mapper) : ITenantService
{
    public async Task<TenantDto> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var tenant = await db.Tenants.FirstOrDefaultAsync(t => t.Id == id, ct)
                     ?? throw new NotFoundException("Tenant not found.");
        return mapper.Map<TenantDto>(tenant);
    }

    public Task<TenantDto> GetCurrentAsync(CancellationToken ct = default)
    {
        var tenantId = currentUser.TenantId
                       ?? throw new UnauthorizedAppException("No tenant in the current context.");
        return GetByIdAsync(tenantId, ct);
    }
}
