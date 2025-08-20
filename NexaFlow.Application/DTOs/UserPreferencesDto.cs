using NexaFlow.Core.Enums;
using NexaFlow.Core.Entities;

namespace NexaFlow.Application.DTOs;

public record UserPreferencesDto(
    ThemeMode ThemeMode,
    string? PrimaryColor,
    string? SecondaryColor,
    string Language
);

public record UpdateUserPreferencesDto(
    ThemeMode ThemeMode,
    string? PrimaryColor,
    string? SecondaryColor,
    string Language
);
