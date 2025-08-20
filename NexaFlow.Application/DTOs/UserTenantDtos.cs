using NexaFlow.Core.Enums;

namespace NexaFlow.Application.DTOs;

public record UserDto(
    Guid Id,
    string Email,
    string FirstName,
    string LastName,
    Guid TenantId,
    bool IsActive,
    IReadOnlyList<string> Roles)
{
    public string FullName => $"{FirstName} {LastName}".Trim();
}

public record TenantDto(
    Guid Id,
    string Name,
    string Slug,
    TenantStatus Status,
    SubscriptionPlan Plan,
    DateTime CreatedAt);

public record InviteMemberRequest(string Email, string RoleName);

public record InvitationDto(
    Guid Id,
    string Email,
    string RoleName,
    InvitationStatus Status,
    DateTime ExpiresAt,
    DateTime CreatedAt);

public record BranchDto(
    Guid Id,
    string Name,
    string? Address,
    string? City,
    string? Phone,
    bool IsHeadquarters);

public record CreateBranchDto(
    string Name,
    string? Address,
    string? City,
    string? Phone,
    bool IsHeadquarters);

public record UpdateBranchDto(
    string Name,
    string? Address,
    string? City,
    string? Phone,
    bool IsHeadquarters);
