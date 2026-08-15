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
    private readonly IOrgScopeService _orgScope;

    public PermissionDelegationService(
        HgsDbContext dbContext,
        IAuditLogService auditLog,
        ILogger<PermissionDelegationService> logger,
        IHttpContextAccessor httpContextAccessor,
        ICacheService cacheService,
        IOrgScopeService orgScope)
    {
        _dbContext = dbContext;
        _auditLog = auditLog;
        _logger = logger;
        _httpContextAccessor = httpContextAccessor;
        _cacheService = cacheService;
        _orgScope = orgScope;
    }

    private int GetCurrentUserId()
    {
        var claim = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(claim, out var userId) ? userId : 0;
    }

    public async Task<IEnumerable<ManageableUserResponse>> GetManageableUsersAsync(CancellationToken cancellationToken = default)
    {
        var currentUserId = GetCurrentUserId();
        if (currentUserId <= 0)
        {
            return Enumerable.Empty<ManageableUserResponse>();
        }

        var scopePaths = await _orgScope.GetCallerScopePathsAsync(cancellationToken);
        if (scopePaths is null)
        {
            return await _dbContext.Users
                .Include(u => u.OrganizationUnit)
                .Where(u => u.IsActive && !u.IsDeleted && u.Id != currentUserId)
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
        }

        if (!scopePaths.Any())
        {
            return Enumerable.Empty<ManageableUserResponse>();
        }

        var manageableUsers = await _dbContext.Users
            .Include(u => u.OrganizationUnit)
            .Where(u => u.IsActive && !u.IsDeleted && u.Id != currentUserId)
            .Where(u => u.OrganizationUnit != null &&
                        u.OrganizationUnit.Path != null &&
                        scopePaths.Any(path => u.OrganizationUnit.Path == path ||
                                               u.OrganizationUnit.Path.StartsWith(path + "/")))
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
        var roles = await _orgScope.GetAssignableRolesAsync(cancellationToken);

        return roles.Select(r => new AssignableRoleResponse
        {
            Id = r.Id,
            Code = r.Code,
            Name = r.Name,
            Description = r.Description,
            OrganizationUnitId = r.OrganizationUnitId ?? 0,
            OrganizationUnitName = r.OrganizationUnit != null ? r.OrganizationUnit.Name : string.Empty
        });
    }

    public async Task AssignRoleAsync(AssignRoleRequest request, CancellationToken cancellationToken = default)
    {
        var currentUserId = GetCurrentUserId();
        
        // Check 1: User has PERMISSIONDELEGATION menu
        if (!await UserHasManagePermissionsMenuAsync(cancellationToken))
        {
            throw new UnauthorizedAccessException("User does not have PERMISSIONDELEGATION menu");
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

        // Check 4: Role must be assignable (active, non-system, org in caller scope)
        if (!await _orgScope.IsRoleAssignableAsync(request.RoleId, cancellationToken))
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
        
        // Check 1: User has PERMISSIONDELEGATION menu
        if (!await UserHasManagePermissionsMenuAsync(cancellationToken))
        {
            throw new UnauthorizedAccessException("User does not have PERMISSIONDELEGATION menu");
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

        // Check 4: Role must be assignable (active, non-system, org in caller scope)
        if (!await _orgScope.IsRoleAssignableAsync(request.RoleId, cancellationToken))
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

        // Check if any of these roles has PERMISSIONDELEGATION menu
        return await _dbContext.RoleMenus
            .Include(rm => rm.Menu)
            .AnyAsync(rm => 
                roleIds.Contains(rm.RoleId) && 
                rm.Menu.Code == "PERMISSIONDELEGATION" &&
                rm.Menu.IsActive, cancellationToken);
    }

    private Task<bool> IsUserInOrgScopeAsync(int targetOrgUnitId, CancellationToken cancellationToken)
    {
        return _orgScope.IsOrgUnitInScopeAsync(targetOrgUnitId, cancellationToken);
    }
}
