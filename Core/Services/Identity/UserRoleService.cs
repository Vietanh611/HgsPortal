using Core.Interfaces;
using Data.DbContexts;
using Domain.Entities.Identity;
using Domain.Entities.System;
using Hgs.Share.Requests.UserRoles;
using Microsoft.EntityFrameworkCore;

namespace Core.Services.Identity;

public class UserRoleService : IUserRoleService
{
    private readonly HgsDbContext _dbContext;
    private readonly IUserMenuService _userMenuService;
    private readonly IAuditLogService _auditLog;
    private readonly ICacheService _cacheService;
    private readonly IOrgScopeService _orgScope;

    public UserRoleService(HgsDbContext dbContext, IUserMenuService userMenuService, IAuditLogService auditLog, ICacheService cacheService, IOrgScopeService orgScope)
    {
        _dbContext = dbContext;
        _userMenuService = userMenuService;
        _auditLog = auditLog;
        _cacheService = cacheService;
        _orgScope = orgScope;
    }

    public async Task<IEnumerable<UserRoles>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.UserRoles
            .Include(ur => ur.User)
            .Include(ur => ur.Role)
            .AsNoTracking()
            .OrderByDescending(ur => ur.AssignedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<UserRoles?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.UserRoles
            .Include(ur => ur.User)
            .Include(ur => ur.Role)
            .AsNoTracking()
            .FirstOrDefaultAsync(ur => ur.Id == id, cancellationToken);
    }

    public async Task<IEnumerable<UserRoles>> GetByUserIdAsync(int userId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.UserRoles
            .Include(ur => ur.User)
            .Include(ur => ur.Role)
            .Where(ur => ur.UserId == userId)
            .AsNoTracking()
            .OrderByDescending(ur => ur.AssignedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<UserRoles>> GetByRoleIdAsync(int roleId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.UserRoles
            .Include(ur => ur.User)
            .Include(ur => ur.Role)
            .Where(ur => ur.RoleId == roleId)
            .AsNoTracking()
            .OrderByDescending(ur => ur.AssignedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<UserRoles> CreateAsync(UserRolesCreateRequest request, int assignedBy, CancellationToken cancellationToken = default)
    {
        // Check if user exists
        var user = await _dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == request.UserId && !u.IsDeleted, cancellationToken);
        if (user is null)
        {
            throw new KeyNotFoundException($"User with ID {request.UserId} not found");
        }

        if (!await _orgScope.IsUserInScopeAsync(request.UserId, cancellationToken))
        {
            throw new UnauthorizedAccessException("Bạn không có quyền thực hiện thao tác này");
        }

        // Check if role exists
        var role = await _dbContext.Roles
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == request.RoleId, cancellationToken);
        if (role is null)
        {
            throw new KeyNotFoundException($"Role with ID {request.RoleId} not found");
        }

        if (!await _orgScope.IsRoleAssignableAsync(request.RoleId, cancellationToken))
        {
            throw new UnauthorizedAccessException("Role is not assignable");
        }

        // Check if user already has this role
        var existingAssignment = await _dbContext.UserRoles
            .AsNoTracking()
            .FirstOrDefaultAsync(ur => ur.UserId == request.UserId && ur.RoleId == request.RoleId, cancellationToken);
        if (existingAssignment is not null)
        {
            throw new InvalidOperationException($"User already has role {role.Name} assigned");
        }

        var userRole = new UserRoles
        {
            UserId = request.UserId,
            RoleId = request.RoleId,
            AssignedAt = DateTime.UtcNow,
            AssignedBy = assignedBy,
        };

        _dbContext.UserRoles.Add(userRole);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _auditLog.Log(
            action: "CREATE",
            entityName: "UserRoles",
            entityId: userRole.Id,
            oldValue: null,
            newValue: userRole);

        await _dbContext.SaveChangesAsync(cancellationToken);

        // Automatically assign all role menus to the user
        await AssignRoleMenusToUserAsync(request.RoleId, request.UserId, assignedBy, cancellationToken);

        await _cacheService.ClearAllMenuCacheAsync(cancellationToken);
        return userRole;
    }

    public async Task<UserRoles?> UpdateAsync(int id, UserRolesUpdateRequest request, CancellationToken cancellationToken = default)
    {
        var userRole = await _dbContext.UserRoles
            .FirstOrDefaultAsync(ur => ur.Id == id, cancellationToken);

        if (userRole is null)
        {
            return null;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        return userRole;
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var userRole = await _dbContext.UserRoles
            .Include(ur => ur.User)
            .FirstOrDefaultAsync(ur => ur.Id == id, cancellationToken);

        if (userRole is null)
        {
            return false;
        }

        // Check if this is the last role for the user
        var userRoleCount = await _dbContext.UserRoles
            .CountAsync(ur => ur.UserId == userRole.UserId, cancellationToken);

        if (userRoleCount <= 1)
        {
            throw new InvalidOperationException("Cannot remove the last role from a user");
        }

        _auditLog.Log(
            action: "DELETE",
            entityName: "UserRoles",
            entityId: userRole.Id,
            oldValue: userRole,
            newValue: null);

        _dbContext.UserRoles.Remove(userRole);
        await _dbContext.SaveChangesAsync(cancellationToken);
        await _cacheService.ClearAllMenuCacheAsync(cancellationToken);
        return true;
    }

    public async Task AssignMultipleRolesAsync(int userId, List<int> roleIds, int assignedBy, DateTime? expiredAt = null, CancellationToken cancellationToken = default)
    {
        // Check if user exists
        var user = await _dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == userId && !u.IsDeleted, cancellationToken);
        if (user is null)
        {
            throw new KeyNotFoundException($"User with ID {userId} not found");
        }

        if (!await _orgScope.IsUserInScopeAsync(userId, cancellationToken))
        {
            throw new UnauthorizedAccessException("Bạn không có quyền thực hiện thao tác này");
        }

        // Get existing role assignments
        var existingRoleIds = await _dbContext.UserRoles
            .Where(ur => ur.UserId == userId)
            .Select(ur => ur.RoleId)
            .ToListAsync(cancellationToken);

        // Filter out already assigned roles
        var newRoleIds = roleIds.Except(existingRoleIds).ToList();

        var createdUserRoles = new List<UserRoles>();
        foreach (var roleId in newRoleIds)
        {
            // Check if role exists
            var role = await _dbContext.Roles
                .AsNoTracking()
                .FirstOrDefaultAsync(r => r.Id == roleId, cancellationToken);
            if (role is null)
            {
                throw new KeyNotFoundException($"Role with ID {roleId} not found");
            }

            if (!await _orgScope.IsRoleAssignableAsync(roleId, cancellationToken))
            {
                throw new UnauthorizedAccessException("Role is not assignable");
            }

            var userRole = new UserRoles
            {
                UserId = userId,
                RoleId = roleId,
                AssignedAt = DateTime.UtcNow,
                AssignedBy = assignedBy,
            };

            _dbContext.UserRoles.Add(userRole);
            createdUserRoles.Add(userRole);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        foreach (var userRole in createdUserRoles)
        {
            _auditLog.Log(
                action: "CREATE",
                entityName: "UserRoles",
                entityId: userRole.Id,
                oldValue: null,
                newValue: userRole);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        // Automatically assign all role menus to the user for each new role
        foreach (var roleId in newRoleIds)
        {
            await AssignRoleMenusToUserAsync(roleId, userId, assignedBy, cancellationToken);
        }

        await _cacheService.ClearAllMenuCacheAsync(cancellationToken);
    }

    public async Task RemoveMultipleRolesAsync(int userId, List<int> roleIds, CancellationToken cancellationToken = default)
    {
        // Check if user exists
        var user = await _dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == userId && !u.IsDeleted, cancellationToken);
        if (user is null)
        {
            throw new KeyNotFoundException($"User with ID {userId} not found");
        }

        if (!await _orgScope.IsUserInScopeAsync(userId, cancellationToken))
        {
            throw new UnauthorizedAccessException("Bạn không có quyền thực hiện thao tác này");
        }

        // Get total role count
        var totalRoleCount = await _dbContext.UserRoles
            .CountAsync(ur => ur.UserId == userId, cancellationToken);

        // Get count of roles to be removed
        var rolesToRemove = await _dbContext.UserRoles
            .Where(ur => ur.UserId == userId && roleIds.Contains(ur.RoleId))
            .ToListAsync(cancellationToken);

        // Check if removing would leave user with no roles
        if (totalRoleCount <= rolesToRemove.Count)
        {
            throw new InvalidOperationException("Cannot remove the last role from a user");
        }

        foreach (var userRole in rolesToRemove)
        {
            _auditLog.Log(
                action: "DELETE",
                entityName: "UserRoles",
                entityId: userRole.Id,
                oldValue: userRole,
                newValue: null);
        }

        _dbContext.UserRoles.RemoveRange(rolesToRemove);
        await _dbContext.SaveChangesAsync(cancellationToken);
        await _cacheService.ClearAllMenuCacheAsync(cancellationToken);
    }

    private async Task AssignRoleMenusToUserAsync(int roleId, int userId, int assignedBy, CancellationToken cancellationToken = default)
    {
        // Get all menus assigned to this role
        var roleMenuIds = await _dbContext.RoleMenus
            .Where(rm => rm.RoleId == roleId)
            .Select(rm => rm.MenuId)
            .ToListAsync(cancellationToken);

        if (!roleMenuIds.Any())
            return;

        // Get existing user menu assignments
        var existingUserMenuIds = await _dbContext.UserMenus
            .Where(um => um.UserId == userId)
            .Select(um => um.MenuId)
            .ToListAsync(cancellationToken);

        // Filter out menus the user already has
        var newMenuIds = roleMenuIds.Except(existingUserMenuIds).ToList();

        // Assign new menus to user
        foreach (var menuId in newMenuIds)
        {
            var userMenu = new UserMenus
            {
                UserId = userId,
                MenuId = menuId,
                AssignedAt = DateTime.UtcNow,
                AssignedBy = assignedBy
            };
            _dbContext.UserMenus.Add(userMenu);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
