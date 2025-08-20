using NexaFlow.Application.DTOs;

namespace NexaFlow.Application.Common.Interfaces;

public interface IRoleService
{
    Task<IReadOnlyList<RoleDto>> GetAllAsync(CancellationToken ct = default);
    Task<RoleDto> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<RoleDto> CreateAsync(CreateRoleDto request, CancellationToken ct = default);
    Task<RoleDto> UpdateAsync(Guid id, UpdateRoleDto request, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
    Task AssignRoleToUserAsync(Guid userId, Guid? roleId, CancellationToken ct = default);
}
