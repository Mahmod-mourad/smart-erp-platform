using System.Security.Cryptography;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using NexaFlow.Application.Common.Exceptions;
using NexaFlow.Application.Common.Interfaces;
using NexaFlow.Application.DTOs;
using NexaFlow.Core.Entities;
using NexaFlow.Core.Enums;
using NexaFlow.Infrastructure.Identity;
using NexaFlow.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;

namespace NexaFlow.Infrastructure.Services;

public class InvitationService(
    AppDbContext db,
    ICurrentUser currentUser,
    UserManager<ApplicationUser> userManager,
    IEmailSender emailSender,
    IMapper mapper,
    IConfiguration config) : IInvitationService
{
    private const int InviteValidDays = 7;

    public async Task<InvitationDto> InviteAsync(InviteMemberRequest request, CancellationToken ct = default)
    {
        var tenantId = currentUser.TenantId
                       ?? throw new UnauthorizedAppException("No tenant in the current context.");

        if (await userManager.FindByEmailAsync(request.Email) is not null)
            throw new ConflictException("A user with this email already exists.");

        var alreadyInvited = await db.Invitations.AnyAsync(
            i => i.Email == request.Email && i.Status == InvitationStatus.Pending, ct);
        if (alreadyInvited)
            throw new ConflictException("A pending invitation already exists for this email.");

        var invite = new TeamInvitation
        {
            TenantId = tenantId,
            Email = request.Email,
            RoleName = request.RoleName,
            Token = GenerateToken(),
            Status = InvitationStatus.Pending,
            ExpiresAt = DateTime.UtcNow.AddDays(InviteValidDays),
            InvitedByUserId = currentUser.UserId!.Value
        };

        db.Invitations.Add(invite);
        await db.SaveChangesAsync(ct);

        await SendInviteEmailAsync(invite, ct);

        return mapper.Map<InvitationDto>(invite);
    }

    public async Task<IReadOnlyList<InvitationDto>> GetPendingAsync(CancellationToken ct = default)
    {
        var items = await db.Invitations
            .Where(i => i.Status == InvitationStatus.Pending)
            .OrderByDescending(i => i.CreatedAt)
            .ToListAsync(ct);
        return mapper.Map<List<InvitationDto>>(items);
    }

    public async Task RevokeAsync(Guid invitationId, CancellationToken ct = default)
    {
        var invite = await db.Invitations.FirstOrDefaultAsync(i => i.Id == invitationId, ct)
                     ?? throw new NotFoundException("Invitation not found.");
        invite.Status = InvitationStatus.Revoked;
        await db.SaveChangesAsync(ct);
    }

    private async Task SendInviteEmailAsync(TeamInvitation invite, CancellationToken ct)
    {
        var baseUrl = config["App:FrontendUrl"] ?? "http://localhost:4200";
        var link = $"{baseUrl}/accept-invite?token={Uri.EscapeDataString(invite.Token)}";
        var html = $"""
            <p>You have been invited to join NexaFlow as <strong>{invite.RoleName}</strong>.</p>
            <p><a href="{link}">Accept the invitation</a> (valid for {InviteValidDays} days).</p>
            """;
        await emailSender.SendAsync(invite.Email, "You're invited to NexaFlow", html, ct);
    }

    private static string GenerateToken() =>
        Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
            .Replace("+", "-").Replace("/", "_").TrimEnd('=');
}
