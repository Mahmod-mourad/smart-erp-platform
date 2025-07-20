using NexaFlow.Core.Common;
using NexaFlow.Core.Enums;

namespace NexaFlow.Core.Entities;

/// <summary>An email invitation for a new member to join a tenant with a given role.</summary>
public class TeamInvitation : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public required string Email { get; set; }
    public required string RoleName { get; set; }
    public required string Token { get; set; }
    public InvitationStatus Status { get; set; } = InvitationStatus.Pending;
    public DateTime ExpiresAt { get; set; }
    public Guid InvitedByUserId { get; set; }
    public DateTime? AcceptedAt { get; set; }

    public bool IsExpired => DateTime.UtcNow >= ExpiresAt;
}
