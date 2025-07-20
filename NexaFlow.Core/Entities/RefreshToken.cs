using NexaFlow.Core.Common;

namespace NexaFlow.Core.Entities;

/// <summary>A rotating refresh token tied to a user. One active per login session.</summary>
public class RefreshToken : BaseEntity
{
    public Guid UserId { get; set; }
    public required string Token { get; set; }
    public DateTime ExpiresAt { get; set; }
    public string? CreatedByIp { get; set; }
    public DateTime? RevokedAt { get; set; }
    public string? ReplacedByToken { get; set; }

    public bool IsExpired => DateTime.UtcNow >= ExpiresAt;
    public bool IsRevoked => RevokedAt is not null;
    public bool IsActive => !IsRevoked && !IsExpired;
}
