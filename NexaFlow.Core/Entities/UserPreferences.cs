using NexaFlow.Core.Common;

namespace NexaFlow.Core.Entities;

public enum ThemeMode
{
    Light = 0,
    Dark = 1,
    Auto = 2
}

public class UserPreferences : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public Guid UserId { get; set; }
    
    public ThemeMode ThemeMode { get; set; } = ThemeMode.Light;
    public string? PrimaryColor { get; set; }
    public string? SecondaryColor { get; set; }
    public string Language { get; set; } = "en";
    
}
