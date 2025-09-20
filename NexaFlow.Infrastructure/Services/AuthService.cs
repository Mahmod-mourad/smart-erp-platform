using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using NexaFlow.Application.Common.Exceptions;
using NexaFlow.Application.Common.Interfaces;
using NexaFlow.Application.DTOs;
using NexaFlow.Core.Constants;
using NexaFlow.Core.Entities;
using NexaFlow.Core.Enums;
using NexaFlow.Infrastructure.Auth;
using NexaFlow.Infrastructure.Identity;
using NexaFlow.Infrastructure.Persistence;

namespace NexaFlow.Infrastructure.Services;

public class AuthService(
    UserManager<ApplicationUser> userManager,
    AppDbContext db,
    IJwtTokenService jwt,
    IOptions<JwtSettings> jwtOptions) : IAuthService
{
    private readonly JwtSettings _jwt = jwtOptions.Value;

    public async Task<AuthResponse> RegisterCompanyAsync(RegisterCompanyRequest request, string? ip, CancellationToken ct = default)
    {
        if (await userManager.FindByEmailAsync(request.AdminEmail) is not null)
            throw new ConflictException("An account with this email already exists.");

        var tenant = new Tenant
        {
            Name = request.CompanyName,
            Slug = await GenerateUniqueSlugAsync(request.CompanyName, ct),
            Status = TenantStatus.Active,
            Plan = SubscriptionPlan.Free
        };
        db.Tenants.Add(tenant);
        await db.SaveChangesAsync(ct);

        var user = new ApplicationUser
        {
            TenantId = tenant.Id,
            UserName = request.AdminEmail,
            Email = request.AdminEmail,
            EmailConfirmed = true,
            FirstName = request.AdminFirstName,
            LastName = request.AdminLastName
        };

        var created = await userManager.CreateAsync(user, request.Password);
        if (!created.Succeeded)
            throw ToValidationException(created);

        await userManager.AddToRoleAsync(user, AppRoles.CompanyAdmin);

        return await BuildAuthResponseAsync(user, ip, ct);
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request, string? ip, CancellationToken ct = default)
    {
        var user = await userManager.FindByEmailAsync(request.Email);
        if (user is null || !await userManager.CheckPasswordAsync(user, request.Password))
            throw new UnauthorizedAppException("Invalid email or password.");

        if (!user.IsActive)
            throw new ForbiddenException("This account has been deactivated.");

        user.LastLoginAt = DateTime.UtcNow;
        await userManager.UpdateAsync(user);

        return await BuildAuthResponseAsync(user, ip, ct);
    }

    public async Task<AuthResponse> RefreshTokenAsync(string refreshToken, string? ip, CancellationToken ct = default)
    {
        var existing = await db.RefreshTokens.FirstOrDefaultAsync(t => t.Token == refreshToken, ct);
        if (existing is null || !existing.IsActive)
            throw new UnauthorizedAppException("Invalid or expired refresh token.");

        var user = await userManager.FindByIdAsync(existing.UserId.ToString())
                   ?? throw new UnauthorizedAppException("Invalid refresh token.");

        // Rotate: revoke the old token and issue a fresh one.
        existing.RevokedAt = DateTime.UtcNow;
        var response = await BuildAuthResponseAsync(user, ip, ct, replacedToken: existing);
        return response;
    }

    public async Task<AuthResponse> AcceptInvitationAsync(AcceptInvitationRequest request, string? ip, CancellationToken ct = default)
    {
        var invite = await db.Invitations.FirstOrDefaultAsync(i => i.Token == request.Token, ct)
                     ?? throw new NotFoundException("Invitation not found.");

        if (invite.Status != InvitationStatus.Pending || invite.IsExpired)
            throw new ConflictException("This invitation is no longer valid.");

        if (await userManager.FindByEmailAsync(invite.Email) is not null)
            throw new ConflictException("An account with this email already exists.");

        var user = new ApplicationUser
        {
            TenantId = invite.TenantId,
            UserName = invite.Email,
            Email = invite.Email,
            EmailConfirmed = true,
            FirstName = request.FirstName,
            LastName = request.LastName
        };

        var created = await userManager.CreateAsync(user, request.Password);
        if (!created.Succeeded)
            throw ToValidationException(created);

        await userManager.AddToRoleAsync(user, invite.RoleName);

        invite.Status = InvitationStatus.Accepted;
        invite.AcceptedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);

        return await BuildAuthResponseAsync(user, ip, ct);
    }

    public async Task RevokeRefreshTokenAsync(string refreshToken, CancellationToken ct = default)
    {
        var token = await db.RefreshTokens.FirstOrDefaultAsync(t => t.Token == refreshToken, ct);
        if (token is { IsActive: true })
        {
            token.RevokedAt = DateTime.UtcNow;
            await db.SaveChangesAsync(ct);
        }
    }

    private async Task<AuthResponse> BuildAuthResponseAsync(
        ApplicationUser user, string? ip, CancellationToken ct, RefreshToken? replacedToken = null)
    {
        var roles = await userManager.GetRolesAsync(user);
        
        var permissions = new List<string>();
        if (user.CustomRoleId.HasValue)
        {
            permissions = await db.RolePermissions
                .Where(p => p.CustomRoleId == user.CustomRoleId)
                .Select(p => p.Permission)
                .ToListAsync(ct);
        }

        var (accessToken, expiresAt) = jwt.GenerateAccessToken(
            user.Id, user.Email!, user.FullName, user.TenantId, roles, permissions);

        var refresh = new RefreshToken
        {
            UserId = user.Id,
            Token = jwt.GenerateRefreshToken(),
            ExpiresAt = DateTime.UtcNow.AddDays(_jwt.RefreshTokenDays),
            CreatedByIp = ip
        };
        db.RefreshTokens.Add(refresh);
        if (replacedToken is not null)
            replacedToken.ReplacedByToken = refresh.Token;
        await db.SaveChangesAsync(ct);

        var dto = new UserDto(user.Id, user.Email!, user.FirstName, user.LastName,
            user.TenantId, user.IsActive, roles.ToList());

        return new AuthResponse(accessToken, refresh.Token, expiresAt, dto);
    }

    private async Task<string> GenerateUniqueSlugAsync(string companyName, CancellationToken ct)
    {
        var slug = SlugGenerator.Slugify(companyName);
        if (!await db.Tenants.AnyAsync(t => t.Slug == slug, ct))
            return slug;
        return SlugGenerator.WithSuffix(slug);
    }

    private static ValidationException ToValidationException(IdentityResult result)
    {
        var errors = result.Errors
            .GroupBy(e => e.Code.Contains("Password") ? "Password" : "Email")
            .ToDictionary(g => g.Key, g => g.Select(e => e.Description).ToArray());
        return new ValidationException(errors);
    }
}
