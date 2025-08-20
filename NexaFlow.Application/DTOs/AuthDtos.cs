namespace NexaFlow.Application.DTOs;

/// <summary>Onboard a brand-new company + its first admin user (T-010).</summary>
public record RegisterCompanyRequest(
    string CompanyName,
    string AdminFirstName,
    string AdminLastName,
    string AdminEmail,
    string Password);

public record LoginRequest(string Email, string Password);

public record RefreshTokenRequest(string RefreshToken);

/// <summary>A new member completing registration from an invite link (T-015).</summary>
public record AcceptInvitationRequest(
    string Token,
    string FirstName,
    string LastName,
    string Password);

public record AuthResponse(
    string AccessToken,
    string RefreshToken,
    DateTime ExpiresAtUtc,
    UserDto User);
