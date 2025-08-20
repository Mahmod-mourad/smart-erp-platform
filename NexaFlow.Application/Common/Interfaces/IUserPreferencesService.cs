using NexaFlow.Application.DTOs;

namespace NexaFlow.Application.Common.Interfaces;

public interface IUserPreferencesService
{
    Task<UserPreferencesDto> GetPreferencesAsync(CancellationToken cancellationToken = default);
    Task<UserPreferencesDto> UpdatePreferencesAsync(UpdateUserPreferencesDto dto, CancellationToken cancellationToken = default);
}
