using Core.Interfaces;
using Data.DbContexts;
using Domain.Entities.Identity;
using Domain.Entities.System;
using Hgs.Share.Requests.PermissionDelegation;
using Hgs.Share.Responses.PermissionDelegation;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Security.Claims;

namespace Core.Services;

public class PermissionDelegationService : IPermissionDelegationService
{
    private readonly HgsDbContext _dbContext;
    private readonly IAuditLogService _auditLog;
    private readonly ILogger<PermissionDelegationService> _logger;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ICacheService _cacheService;

    public PermissionDelegationService(
        HgsDbContext dbContext,
        IAuditLogService auditLog,
        ILogger<PermissionDelegationService> logger,
        IHttpContextAccessor httpContextAccessor,
        ICacheService cacheService)
    {
        _dbContext = dbContext;
        _auditLog = auditLog;
        _logger = logger;
        _httpContextAccessor = httpContextAccessor;
        _cacheService = cacheService;
    }

    private int GetCurrentUserId()
    {
        var claim = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(claim, out var userId) ? userId : 0;
    }

    public async Task<IEnumerable<ManageableUserResponse>> GetManageableUsersAsync(CancellationToken cancellationToken = default)
    {
        var currentUserId = GetCurrentUserId();
        
        // Get organization units the current user manages through their roles
        var userRoleOrgUnits = await _dbContext.UserRoles
            .Include(ur => ur.Role)
            .Where(ur => ur.UserId == currentUserId && ur.Role.IsActive)
            .Select(ur => ur.Role.OrganizationUnitId)
            .Distinct()
            .ToListAsync(cancellationToken);

        if (!userRoleOrgUnits.Any())
            return Enumerable.Empty<ManageableUserResponse>();

        // Get all organization unit paths for these org units
        var orgUnitPaths = await _dbContext.OrganizationUnits
            .Where(ou => userRoleOrgUnits.Contains(ou.Id))
            .Select(ou => ou.Path)
            .ToListAsync(cancellationToken);

        // Get users whose organization unit path starts with any of the manager's org unit paths
        var manageableUsers = await _dbContext.Users
            .Include(u => u.OrganizationUnit)
            .Where(u => u.IsActive && !u.IsDeleted)
            .Where(u => userRoleOrgUnits.Contains(u.OrganizationUnitId) == false) // Exclude users in same org as manager
            .Where(u => orgUnitPaths.Any(path => 
                u.OrganizationUnit != null && u.OrganizationUnit.Path != null && 
                (u.OrganizationUnit.Path == path || u.OrganizationUnit.Path.StartsWith(path + "/"))))
            .Select(u => new ManageableUserResponse
            {
                Id = u.Id,
                Username = u.Username,
                Email = u.Email,
                FullName = u.FullName,
                OrganizationUnitId = u.OrganizationUnitId,
                OrganizationUnitName = u.OrganizationUnit != null ? u.OrganizationUnit.Name : string.Empty,
                IsActive = u.IsActive
            })
            .ToListAsync(cancellationToken);

        return manageableUsers;
    }

    public async Task<IEnumerable<AssignableRoleResponse>> GetAssignableRolesAsync(CancellationToken cancellationToken = default)
    {
        var currentUserId = GetCurrentUserId();
        
        // Get roles that the current user has
        var userRoleIds = await _dbContext.UserRoles
            .Where(ur => ur.UserId == currentUserId)
            .Select(ur => ur.RoleId)
            .ToListAsync(cancellationToken);

        if (!userRoleIds.Any())
            return Enumerable.Empty<AssignableRoleResponse>();

        var assignableRoles = await _dbContext.Roles
            .Include(r => r.OrganizationUnit)
            .Where(r => userRoleIds.Contains(r.Id) && r.IsActive)
            .Select(r => new AssignableRoleResponse
            {
                Id = r.Id,
                Code = r.Code,
                Name = r.Name,
                Description = r.Description,
                OrganizationUnitId = r.OrganizationUnitId ?? 0,
                OrganizationUnitName = r.OrganizationUnit != null ? r.OrganizationUnit.Name : string.Empty
            })
            .ToListAsync(cancellationToken);

        return assignableRoles;
    }

    public async Task AssignRoleAsync(AssignRoleRequest request, CancellationToken cancellationToken = default)
    {
        var currentUserId = GetCurrentUserId();
        
        // Check 1: User has MANAGE_PERMISSIONS menu
        if (!await UserHasManagePermissionsMenuAsync(cancellationToken))
        {
            throw new UnauthorizedAccessException("User does not have MANAGE_PERMISSIONS menu");
        }

        // Check 2: Cannot assign to self
        if (request.TargetUserId == currentUserId)
        {
            throw new UnauthorizedAccessException("Cannot assign role to self");
        }

        // Check 3: Target user must be in organizational scope
        var targetUser = await _dbContext.Users
            .Include(u => u.OrganizationUnit)
            .FirstOrDefaultAsync(u => u.Id == request.TargetUserId, cancellationToken);
        
        if (targetUser == null || targetUser.OrganizationUnit == null)
        {
            throw new KeyNotFoundException("Target user not found");
        }

        if (!await IsUserInOrgScopeAsync(targetUser.OrganizationUnitId, cancellationToken))
        {
            throw new UnauthorizedAccessException("Target user is not in organizational scope");
        }

        // Check 4: Role must be in user's assignable roles
        if (!await IsRoleAssignableAsync(request.RoleId, cancellationToken))
        {
            throw new UnauthorizedAccessException("Role is not assignable");
        }

        // Check 5: User doesn't already have this role
        var existingRole = await _dbContext.UserRoles
            .FirstOrDefaultAsync(ur => ur.UserId == request.TargetUserId && ur.RoleId == request.RoleId, cancellationToken);
        
        if (existingRole != null)
        {
            return; // Already has role, nothing to do
        }

        // Assign role
        var userRole = new UserRoles
        {
            UserId = request.TargetUserId,
            RoleId = request.RoleId,
            AssignedAt = DateTime.UtcNow
        };

        _dbContext.UserRoles.Add(userRole);
        
        // Audit log
        _auditLog.Log("CREATE", "UserRoles", request.TargetUserId, null, new { UserId = request.TargetUserId, RoleId = request.RoleId });
        
        await _dbContext.SaveChangesAsync(cancellationToken);
        
        // Clear menu cache for target user since their roles changed
        await _cacheService.ClearUserMenuCacheAsync(request.TargetUserId, cancellationToken);
        
        _logger.LogInformation("User {CurrentUserId} assigned role {RoleId} to user {TargetUserId}", currentUserId, request.RoleId, request.TargetUserId);
    }

    public async Task RevokeRoleAsync(RevokeRoleRequest request, CancellationToken cancellationToken = default)
    {
        var currentUserId = GetCurrentUserId();
        
        // Check 1: User has MANAGE_PERMISSIONS menu
        if (!await UserHasManagePermissionsMenuAsync(cancellationToken))
        {
            throw new UnauthorizedAccessException("User does not have MANAGE_PERMISSIONS menu");
        }

        // Check 2: Cannot revoke from self
        if (request.TargetUserId == currentUserId)
        {
            throw new UnauthorizedAccessException("Cannot revoke role from self");
        }

        // Check 3: Target user must be in organizational scope
        var targetUser = await _dbContext.Users
            .Include(u => u.OrganizationUnit)
            .FirstOrDefaultAsync(u => u.Id == request.TargetUserId, cancellationToken);
        
        if (targetUser == null || targetUser.OrganizationUnit == null)
        {
            throw new KeyNotFoundException("Target user not found");
        }

        if (!await IsUserInOrgScopeAsync(targetUser.OrganizationUnitId, cancellationToken))
        {
            throw new UnauthorizedAccessException("Target user is not in organizational scope");
        }

        // Check 4: Role must be in user's assignable roles
        if (!await IsRoleAssignableAsync(request.RoleId, cancellationToken))
        {
            throw new UnauthorizedAccessException("Role is not assignable");
        }

        // Check 5: User has this role
        var userRole = await _dbContext.UserRoles
            .FirstOrDefaultAsync(ur => ur.UserId == request.TargetUserId && ur.RoleId == request.RoleId, cancellationToken);
        
        if (userRole == null)
        {
            return; // Doesn't have role, nothing to do
        }

        // Revoke role
        _dbContext.UserRoles.Remove(userRole);
        
        // Audit log
        _auditLog.Log("DELETE", "UserRoles", request.TargetUserId, new { UserId = request.TargetUserId, RoleId = request.RoleId }, null);
        
        await _dbContext.SaveChangesAsync(cancellationToken);
        
        // Clear menu cache for target user since their roles changed
        await _cacheService.ClearUserMenuCacheAsync(request.TargetUserId, cancellationToken);
        
        _logger.LogInformation("User {CurrentUserId} revoked role {RoleId} from user {TargetUserId}", currentUserId, request.RoleId, request.TargetUserId);
    }

    public async Task<UserEffectivePermissionsResponse?> GetUserEffectivePermissionsAsync(int userId, CancellationToken cancellationToken = default)
    {
        var user = await _dbContext.Users
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
        
        if (user == null)
            return null;

        // Get user's roles
        var userRoles = await _dbContext.UserRoles
            .Include(ur => ur.Role)
            .Where(ur => ur.UserId == userId)
            .Select(ur => new RoleInfo
            {
                Id = ur.Role.Id,
                Code = ur.Role.Code,
                Name = ur.Role.Name
            })
            .ToListAsync(cancellationToken);

        // Get user's menus through roles
        var roleIds = userRoles.Select(r => r.Id).ToList();
        var menus = await _dbContext.RoleMenus
            .Include(rm => rm.Menu)
            .Where(rm => roleIds.Contains(rm.RoleId) && rm.Menu.IsActive)
            .Select(rm => new MenuInfo
            {
                Id = rm.Menu.Id,
                Code = rm.Menu.Code,
                Name = rm.Menu.Name
            })
            .Distinct()
            .ToListAsync(cancellationToken);

        return new UserEffectivePermissionsResponse
        {
            UserId = user.Id,
            Username = user.Username,
            FullName = user.FullName,
            Roles = userRoles,
            Menus = menus
        };
    }

    private async Task<bool> UserHasManagePermissionsMenuAsync(CancellationToken cancellationToken)
    {
        var currentUserId = GetCurrentUserId();
        
        // Get user's roles
        var roleIds = await _dbContext.UserRoles
            .Where(ur => ur.UserId == currentUserId)
            .Select(ur => ur.RoleId)
            .ToListAsync(cancellationToken);

        // Check if any of these roles has MANAGE_PERMISSIONS menu
        return await _dbContext.RoleMenus
            .Include(rm => rm.Menu)
            .AnyAsync(rm => 
                roleIds.Contains(rm.RoleId) && 
                rm.Menu.Code == "MANAGE_PERMISSIONS" &&
                rm.Menu.IsActive, cancellationToken);
    }

    private async Task<bool> IsUserInOrgScopeAsync(int targetOrgUnitId, CancellationToken cancellationToken)
    {
        var currentUserId = GetCurrentUserId();
        
        // Get organization units the current user manages through their roles
        var userRoleOrgUnits = await _dbContext.UserRoles
            .Include(ur => ur.Role)
            .Where(ur => ur.UserId == currentUserId && ur.Role.IsActive)
            .Select(ur => ur.Role.OrganizationUnitId)
            .Distinct()
            .ToListAsync(cancellationToken);

        if (!userRoleOrgUnits.Any())
            return false;

        // Get paths for these org units
        var orgUnitPaths = await _dbContext.OrganizationUnits
            .Where(ou => userRoleOrgUnits.Contains(ou.Id))
            .Select(ou => ou.Path)
            .ToListAsync(cancellationToken);

        // Check if target org unit is in scope
        var targetOrgUnit = await _dbContext.OrganizationUnits
            .FirstOrDefaultAsync(ou => ou.Id == targetOrgUnitId, cancellationToken);

        if (targetOrgUnit == null || string.IsNullOrEmpty(targetOrgUnit.Path))
            return false;

        return orgUnitPaths.Any(path => 
            targetOrgUnit.Path == path || targetOrgUnit.Path.StartsWith(path + "/"));
    }

    private async Task<bool> IsRoleAssignableAsync(int roleId, CancellationToken cancellationToken)
    {
        var currentUserId = GetCurrentUserId();
        
        // Get roles the current user has
        var userRoleIds = await _dbContext.UserRoles
            .Where(ur => ur.UserId == currentUserId)
            .Select(ur => ur.RoleId)
            .ToListAsync(cancellationToken);

        return userRoleIds.Contains(roleId);
    }
}
