using Microsoft.EntityFrameworkCore;
using NexaFlow.Application.Common.Interfaces;
using NexaFlow.Application.DTOs;
using NexaFlow.Core.Entities;
using NexaFlow.Infrastructure.Persistence;

namespace NexaFlow.Infrastructure.Services;

public class UserPreferencesService(AppDbContext db, ICurrentUser currentUser, ICacheService cacheService) : IUserPreferencesService
{
    private string GetCacheKey(Guid userId) => $"user_prefs_{userId}";
    public async Task<UserPreferencesDto> GetPreferencesAsync(CancellationToken cancellationToken = default)
    {
        var userId = currentUser.UserId ?? Guid.Empty;
        if (userId == Guid.Empty) return new UserPreferencesDto(ThemeMode.Light, null, null, "en");

        var cacheKey = GetCacheKey(userId);
        var cachedPrefs = await cacheService.GetAsync<UserPreferencesDto>(cacheKey, cancellationToken);
        if (cachedPrefs != null)
        {
            return cachedPrefs;
        }

        var prefs = await db.UserPreferences
            .FirstOrDefaultAsync(p => p.UserId == userId, cancellationToken);

        var dto = prefs == null 
            ? new UserPreferencesDto(ThemeMode.Light, null, null, "en")
            : new UserPreferencesDto(prefs.ThemeMode, prefs.PrimaryColor, prefs.SecondaryColor, prefs.Language);

        await cacheService.SetAsync(cacheKey, dto, TimeSpan.FromHours(24), cancellationToken);

        return dto;
    }

    public async Task<UserPreferencesDto> UpdatePreferencesAsync(UpdateUserPreferencesDto dto, CancellationToken cancellationToken = default)
    {
        var userId = currentUser.UserId ?? throw new UnauthorizedAccessException();

        var prefs = await db.UserPreferences
            .FirstOrDefaultAsync(p => p.UserId == userId, cancellationToken);

        if (prefs == null)
        {
            prefs = new UserPreferences
            {
                UserId = userId,
                ThemeMode = dto.ThemeMode,
                PrimaryColor = dto.PrimaryColor,
                SecondaryColor = dto.SecondaryColor,
                Language = dto.Language
            };
            db.UserPreferences.Add(prefs);
        }
        else
        {
            prefs.ThemeMode = dto.ThemeMode;
            prefs.PrimaryColor = dto.PrimaryColor;
            prefs.SecondaryColor = dto.SecondaryColor;
            prefs.Language = dto.Language;
        }

        await db.SaveChangesAsync(cancellationToken);

        var dtoDto = new UserPreferencesDto(prefs.ThemeMode, prefs.PrimaryColor, prefs.SecondaryColor, prefs.Language);
        await cacheService.SetAsync(GetCacheKey(userId), dtoDto, TimeSpan.FromHours(24), cancellationToken);

        return dtoDto;
    }
}
