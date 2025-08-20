using NexaFlow.Application.DTOs;

namespace NexaFlow.Application.Common.Interfaces;

public interface IBranchService
{
    Task<BranchDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<BranchDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<BranchDto> CreateAsync(CreateBranchDto dto, CancellationToken cancellationToken = default);
    Task<BranchDto> UpdateAsync(Guid id, UpdateBranchDto dto, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
