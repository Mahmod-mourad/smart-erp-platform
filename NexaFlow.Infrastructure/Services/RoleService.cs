using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using NexaFlow.Application.Common.Exceptions;
using NexaFlow.Application.Common.Interfaces;
using NexaFlow.Application.DTOs;
using NexaFlow.Core.Entities;
using NexaFlow.Infrastructure.Identity;
using NexaFlow.Infrastructure.Persistence;

namespace NexaFlow.Infrastructure.Services;

public class RoleService(AppDbContext db, UserManager<ApplicationUser> userManager) : IRoleService
{
    public async Task<IReadOnlyList<RoleDto>> GetAllAsync(CancellationToken ct = default)
    {
        var roles = await db.CustomRoles
            .Include(r => r.Permissions)
            .AsNoTracking()
            .ToListAsync(ct);

        return roles.Select(Map).ToList();
    }

    public async Task<RoleDto> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var role = await db.CustomRoles
            .Include(r => r.Permissions)
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == id, ct)
            ?? throw new NotFoundException("Role not found.");

        return Map(role);
    }

    public async Task<RoleDto> CreateAsync(CreateRoleDto request, CancellationToken ct = default)
    {
        var role = new CustomRole
        {
            Name = request.Name,
            Description = request.Description
        };

        foreach (var perm in request.Permissions)
        {
            role.Permissions.Add(new RolePermission { Permission = perm });
        }

        db.CustomRoles.Add(role);
        await db.SaveChangesAsync(ct);

        return Map(role);
    }

    public async Task<RoleDto> UpdateAsync(Guid id, UpdateRoleDto request, CancellationToken ct = default)
    {
        var role = await db.CustomRoles
            .Include(r => r.Permissions)
            .FirstOrDefaultAsync(r => r.Id == id, ct)
            ?? throw new NotFoundException("Role not found.");

        role.Name = request.Name;
        role.Description = request.Description;

        // Sync permissions
        var existingPerms = role.Permissions.Select(p => p.Permission).ToList();
        
        // Add new
        var toAdd = request.Permissions.Except(existingPerms);
        foreach (var p in toAdd)
        {
            role.Permissions.Add(new RolePermission { Permission = p });
        }

        // Remove old
        var toRemove = role.Permissions.Where(p => !request.Permissions.Contains(p.Permission)).ToList();
        foreach (var p in toRemove)
        {
            role.Permissions.Remove(p);
        }

        await db.SaveChangesAsync(ct);

        return Map(role);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var role = await db.CustomRoles.FindAsync([id], ct)
            ?? throw new NotFoundException("Role not found.");

        db.CustomRoles.Remove(role);
        await db.SaveChangesAsync(ct);
    }

    public async Task AssignRoleToUserAsync(Guid userId, Guid? roleId, CancellationToken ct = default)
    {
        var user = await userManager.FindByIdAsync(userId.ToString())
            ?? throw new NotFoundException("User not found.");

        if (roleId.HasValue)
        {
            var roleExists = await db.CustomRoles.AnyAsync(r => r.Id == roleId.Value, ct);
            if (!roleExists) throw new NotFoundException("Role not found.");
        }

        user.CustomRoleId = roleId;
        await userManager.UpdateAsync(user);
    }

    private static RoleDto Map(CustomRole role) =>
        new(role.Id, role.Name, role.Description, role.Permissions.Select(p => p.Permission).ToList());
}
