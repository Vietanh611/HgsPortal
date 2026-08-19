using Core.Constants;
using Core.Interfaces.Identity;
using Core.Interfaces.Notifications;
using Core.Interfaces.Operations;
using Data.DbContexts;
using Domain.Entities.Identity;
using Domain.Entities.System;
using Hgs.Share.Requests.Notifications;
using Hgs.Share.Requests.PermissionDelegation;
using Hgs.Share.Responses.PermissionDelegation;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Security.Claims;

namespace Core.Services.Identity;

public class PermissionDelegationService : IPermissionDelegationService
{
    private readonly HgsDbContext _dbContext;
    private readonly IAuditLogService _auditLog;
    private readonly INotificationService _notificationService;
    private readonly ILogger<PermissionDelegationService> _logger;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ICacheService _cacheService;
    private readonly IOrgScopeService _orgScope;

    public PermissionDelegationService(
        HgsDbContext dbContext,
        IAuditLogService auditLog,
        INotificationService notificationService,
        ILogger<PermissionDelegationService> logger,
        IHttpContextAccessor httpContextAccessor,
        ICacheService cacheService,
        IOrgScopeService orgScope)
    {
        _dbContext = dbContext;
        _auditLog = auditLog;
        _notificationService = notificationService;
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

        return await _dbContext.Users
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
    }

    public async Task<IEnumerable<AssignableRoleResponse>> GetAssignableRolesAsync(CancellationToken cancellationToken = default)
    {
        var roles = await GetAssignableRolesInternalAsync(cancellationToken);

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

    private async Task<IEnumerable<Roles>> GetAssignableRolesInternalAsync(CancellationToken cancellationToken)
    {
        var currentUserId = GetCurrentUserId();
        if (await IsCurrentUserSuperAdminAsync(currentUserId, cancellationToken))
        {
            return await _dbContext.Roles
                .AsNoTracking()
                .Include(r => r.OrganizationUnit)
                .Where(r => r.IsActive && !r.IsSystemRole)
                .OrderBy(r => r.Name)
                .ToListAsync(cancellationToken);
        }

        if (currentUserId <= 0)
        {
            return Enumerable.Empty<Roles>();
        }

        // "Chọn quyền gán" chỉ hiển thị những role caller đang giữ — không được ủy quyền vượt quyền của mình
        var heldRoleIds = _dbContext.UserRoles
            .AsNoTracking()
            .Where(ur => ur.UserId == currentUserId)
            .Select(ur => ur.RoleId);

        return await _dbContext.Roles
            .AsNoTracking()
            .Include(r => r.OrganizationUnit)
            .Where(r => heldRoleIds.Contains(r.Id) && r.IsActive && !r.IsSystemRole)
            .OrderBy(r => r.Name)
            .ToListAsync(cancellationToken);
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

        // Check 3: Target user must exist, not be deleted and be in organizational scope
        var targetUser = await _dbContext.Users
            .Include(u => u.OrganizationUnit)
            .FirstOrDefaultAsync(u => u.Id == request.TargetUserId && !u.IsDeleted, cancellationToken);

        if (targetUser == null || targetUser.OrganizationUnit == null)
        {
            throw new KeyNotFoundException("Target user not found");
        }

        if (!await IsUserInOrgScopeAsync(targetUser.OrganizationUnitId, cancellationToken))
        {
            throw new UnauthorizedAccessException("Target user is not in organizational scope");
        }

        // Check 4: Role must be assignable (active, non-system — không giới hạn theo org)
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

        var delegatedRole = await _dbContext.Roles.AsNoTracking().FirstOrDefaultAsync(r => r.Id == request.RoleId, cancellationToken);

        // Audit — thay thế Log CRUD cũ (không thêm dòng trùng)
        await _auditLog.LogSecurityEventAsync(
            action: "PERMISSION_DELEGATED",
            eventCategory: "Permission", success: true, severity: "Critical",
            userId: currentUserId,               // người ủy quyền
            targetUserId: request.TargetUserId,  // user nhận ủy quyền
            entityName: "Roles",
            entityId: request.RoleId,
            detail: $"Ủy quyền role '{delegatedRole?.Name}' cho user '{targetUser.Username}'",
            newValue: new { roleId = request.RoleId, delegatedRole?.Name, targetUserId = request.TargetUserId });

        await _dbContext.SaveChangesAsync(cancellationToken);
        
        // Clear menu cache for target user since their roles changed
        await _cacheService.ClearUserMenuCacheAsync(request.TargetUserId, cancellationToken);

        // Thông báo cho chính user nhận ủy quyền + SUPER_ADMIN
        await TryNotifyAsync(() => NotifyDelegationAsync(targetUser.Username, delegatedRole?.Name, request.TargetUserId));

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

        // Check 3: Target user must exist, not be deleted and be in organizational scope
        var targetUser = await _dbContext.Users
            .Include(u => u.OrganizationUnit)
            .FirstOrDefaultAsync(u => u.Id == request.TargetUserId && !u.IsDeleted, cancellationToken);

        if (targetUser == null || targetUser.OrganizationUnit == null)
        {
            throw new KeyNotFoundException("Target user not found");
        }

        if (!await IsUserInOrgScopeAsync(targetUser.OrganizationUnitId, cancellationToken))
        {
            throw new UnauthorizedAccessException("Target user is not in organizational scope");
        }

        // Check 4: Role must be assignable (active, non-system — không giới hạn theo org)
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

        var revokedRole = await _dbContext.Roles.AsNoTracking().FirstOrDefaultAsync(r => r.Id == request.RoleId, cancellationToken);

        // Audit — thay thế Log CRUD cũ (không thêm dòng trùng)
        await _auditLog.LogSecurityEventAsync(
            action: "PERMISSION_DELEGATION_REVOKED",
            eventCategory: "Permission", success: true, severity: "Warning",
            userId: currentUserId,
            targetUserId: request.TargetUserId,
            entityName: "Roles",
            entityId: request.RoleId,
            detail: $"Thu hồi ủy quyền role '{revokedRole?.Name}' của user '{targetUser.Username}'",
            oldValue: new { roleId = request.RoleId, revokedRole?.Name, targetUserId = request.TargetUserId });

        await _dbContext.SaveChangesAsync(cancellationToken);
        
        // Clear menu cache for target user since their roles changed
        await _cacheService.ClearUserMenuCacheAsync(request.TargetUserId, cancellationToken);

        // Thông báo cho chính user bị thu hồi + SUPER_ADMIN
        await TryNotifyAsync(() => NotifyDelegationRevokedAsync(targetUser.Username, revokedRole?.Name, request.TargetUserId));

        _logger.LogInformation("User {CurrentUserId} revoked role {RoleId} from user {TargetUserId}", currentUserId, request.RoleId, request.TargetUserId);
    }

    /// <summary>
    /// Ủy quyền theo "tập đầy đủ" (replace semantics): gán role chưa có, thu hồi role đang giữ mà không
    /// còn trong danh sách — role hệ thống được giữ nguyên (không thu hồi). User đích phải thuộc phạm vi
    /// tổ chức của caller; role chỉ được chọn trong tập role caller đang giữ (SUPER_ADMIN nhận tất cả).
    /// Tương ứng mô hình tick/bỏ checkbox trên giao diện.
    /// </summary>
    public async Task AssignRolesAsync(AssignRolesRequest request, CancellationToken cancellationToken = default)
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

        // Check 3: Target user must exist, not be deleted and be in organizational scope
        var targetUser = await _dbContext.Users
            .Include(u => u.OrganizationUnit)
            .FirstOrDefaultAsync(u => u.Id == request.TargetUserId && !u.IsDeleted, cancellationToken);

        if (targetUser == null || targetUser.OrganizationUnit == null)
        {
            throw new KeyNotFoundException("Target user not found");
        }

        if (!await IsUserInOrgScopeAsync(targetUser.OrganizationUnitId, cancellationToken))
        {
            throw new UnauthorizedAccessException("Target user is not in organizational scope");
        }

        var requestedRoleIds = request.RoleIds.Distinct().ToList();

        // Check 4: every requested role must be assignable (active, non-system — không giới hạn theo org)
        var assignableRoleIds = await GetAssignableRoleIdsAsync(cancellationToken);
        if (requestedRoleIds.Except(assignableRoleIds).Any())
        {
            throw new UnauthorizedAccessException("Role is not assignable");
        }

        var currentUserRoles = await _dbContext.UserRoles
            .Include(ur => ur.Role)
            .Where(ur => ur.UserId == request.TargetUserId)
            .ToListAsync(cancellationToken);

        var currentlyHeldIds = currentUserRoles.Select(ur => ur.RoleId).ToHashSet();

        var toAddRoleIds = requestedRoleIds.Where(id => !currentlyHeldIds.Contains(id)).ToList();
        var toRemoveRoleIds = currentUserRoles
            .Where(ur => assignableRoleIds.Contains(ur.RoleId) && !requestedRoleIds.Contains(ur.RoleId))
            .Select(ur => ur.RoleId)
            .ToList();

        if (!toAddRoleIds.Any() && !toRemoveRoleIds.Any())
        {
            return;
        }

        var toRemoveUserRoles = currentUserRoles
            .Where(ur => toRemoveRoleIds.Contains(ur.RoleId))
            .ToList();

        foreach (var roleId in toAddRoleIds)
        {
            _dbContext.UserRoles.Add(new UserRoles
            {
                UserId = request.TargetUserId,
                RoleId = roleId,
                AssignedAt = DateTime.UtcNow
            });
        }

        _dbContext.UserRoles.RemoveRange(toRemoveUserRoles);
        await _dbContext.SaveChangesAsync(cancellationToken);

        var addedRoleNames = await _dbContext.Roles.AsNoTracking()
            .Where(r => toAddRoleIds.Contains(r.Id))
            .ToDictionaryAsync(r => r.Id, r => r.Name, cancellationToken);
        var removedRoleNames = await _dbContext.Roles.AsNoTracking()
            .Where(r => toRemoveRoleIds.Contains(r.Id))
            .ToDictionaryAsync(r => r.Id, r => r.Name, cancellationToken);

        foreach (var roleId in toAddRoleIds)
        {
            await _auditLog.LogSecurityEventAsync(
                action: "PERMISSION_DELEGATED",
                eventCategory: "Permission", success: true, severity: "Critical",
                userId: currentUserId,               // người ủy quyền
                targetUserId: request.TargetUserId,  // user nhận ủy quyền
                entityName: "Roles",
                entityId: roleId,
                detail: $"Ủy quyền role '{addedRoleNames.GetValueOrDefault(roleId)}' cho user '{targetUser.Username}'",
                newValue: new { roleId, roleName = addedRoleNames.GetValueOrDefault(roleId), targetUserId = request.TargetUserId });
        }

        foreach (var roleId in toRemoveRoleIds)
        {
            await _auditLog.LogSecurityEventAsync(
                action: "PERMISSION_DELEGATION_REVOKED",
                eventCategory: "Permission", success: true, severity: "Warning",
                userId: currentUserId,
                targetUserId: request.TargetUserId,
                entityName: "Roles",
                entityId: roleId,
                detail: $"Thu hồi ủy quyền role '{removedRoleNames.GetValueOrDefault(roleId)}' của user '{targetUser.Username}'",
                oldValue: new { roleId, roleName = removedRoleNames.GetValueOrDefault(roleId), targetUserId = request.TargetUserId });
        }

        await _cacheService.ClearUserMenuCacheAsync(request.TargetUserId, cancellationToken);

        foreach (var roleId in toAddRoleIds)
        {
            await TryNotifyAsync(() => NotifyDelegationAsync(targetUser.Username, addedRoleNames.GetValueOrDefault(roleId), request.TargetUserId));
        }

        foreach (var roleId in toRemoveRoleIds)
        {
            await TryNotifyAsync(() => NotifyDelegationRevokedAsync(targetUser.Username, removedRoleNames.GetValueOrDefault(roleId), request.TargetUserId));
        }

        _logger.LogInformation("User {CurrentUserId} updated delegated roles for user {TargetUserId}: +{AddCount} -{RemoveCount}",
            currentUserId, request.TargetUserId, toAddRoleIds.Count, toRemoveRoleIds.Count);
    }

    public async Task<UserEffectivePermissionsResponse?> GetUserEffectivePermissionsAsync(int userId, CancellationToken cancellationToken = default)
    {
        var user = await _dbContext.Users
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

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

    private async Task<bool> IsRoleAssignableAsync(int roleId, CancellationToken cancellationToken)
    {
        var role = await _dbContext.Roles
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == roleId, cancellationToken);
        if (role is not { IsActive: true, IsSystemRole: false })
        {
            return false;
        }

        var currentUserId = GetCurrentUserId();
        if (await IsCurrentUserSuperAdminAsync(currentUserId, cancellationToken))
        {
            return true;
        }

        return currentUserId > 0 && await _dbContext.UserRoles
            .AsNoTracking()
            .AnyAsync(ur => ur.UserId == currentUserId && ur.RoleId == roleId, cancellationToken);
    }

    private async Task<List<int>> GetAssignableRoleIdsAsync(CancellationToken cancellationToken)
    {
        var currentUserId = GetCurrentUserId();
        if (await IsCurrentUserSuperAdminAsync(currentUserId, cancellationToken))
        {
            return await _dbContext.Roles
                .AsNoTracking()
                .Where(r => r.IsActive && !r.IsSystemRole)
                .Select(r => r.Id)
                .ToListAsync(cancellationToken);
        }

        if (currentUserId <= 0)
        {
            return new List<int>();
        }

        return await _dbContext.UserRoles
            .AsNoTracking()
            .Where(ur => ur.UserId == currentUserId && ur.Role != null && ur.Role.IsActive && !ur.Role.IsSystemRole)
            .Select(ur => ur.RoleId)
            .Distinct()
            .ToListAsync(cancellationToken);
    }

    private async Task<bool> IsCurrentUserSuperAdminAsync(int currentUserId, CancellationToken cancellationToken)
    {
        if (currentUserId <= 0)
        {
            return false;
        }

        return await _dbContext.UserRoles
            .AsNoTracking()
            .AnyAsync(ur => ur.UserId == currentUserId
                && ur.Role != null
                && ur.Role.Code == RoleCodes.SuperAdmin
                && ur.Role.IsActive, cancellationToken);
    }

    private async Task NotifyDelegationAsync(string targetUsername, string? roleName, int targetUserId)
    {
        var currentUserId = GetCurrentUserId();

        // Case 1 — user bị tác động: ủy quyền ghi UserRoles trực tiếp (không qua UserRoleService)
        // nên chính nhánh này phải báo cho target biết vai trò của họ vừa đổi
        await _notificationService.NotifyUsersAsync(new NotifyRequest
        {
            Category = NotificationCategories.Permission,
            Severity = "Warning",
            Title = "Bạn được ủy quyền vai trò mới",
            Body = $"Bạn vừa được ủy quyền vai trò '{roleName}'. Kiểm tra quyền hạn thực tế của bạn.",
            ActionUrl = "/permission-delegation",
            SourceEntityName = "UserRoles",
            SourceEntityId = targetUserId.ToString(),
            TriggeredByUserId = currentUserId
        }, new[] { targetUserId });

        // Case 2 — xác nhận cho người thao tác (không broadcast SUPER_ADMIN; theo dõi qua audit log)
        if (currentUserId > 0)
        {
            await _notificationService.NotifyUsersAsync(new NotifyRequest
            {
                Category = NotificationCategories.Permission,
                Severity = "Info",
                Title = "Đã ủy quyền vai trò",
                Body = $"Bạn vừa ủy quyền vai trò '{roleName}' cho user '{targetUsername}'.",
                ActionUrl = "/permission-delegation",
                SourceEntityName = "UserRoles",
                SourceEntityId = targetUserId.ToString(),
                TriggeredByUserId = currentUserId
            }, new[] { currentUserId });
        }
    }

    private async Task NotifyDelegationRevokedAsync(string targetUsername, string? roleName, int targetUserId)
    {
        var currentUserId = GetCurrentUserId();

        // Case 1 — user bị tác động biết vai trò của họ vừa bị thu hồi
        await _notificationService.NotifyUsersAsync(new NotifyRequest
        {
            Category = NotificationCategories.Permission,
            Severity = "Info",
            Title = "Ủy quyền vai trò bị thu hồi",
            Body = $"Vai trò '{roleName}' ủy quyền cho bạn vừa bị thu hồi.",
            ActionUrl = "/permission-delegation",
            SourceEntityName = "UserRoles",
            SourceEntityId = targetUserId.ToString(),
            TriggeredByUserId = currentUserId
        }, new[] { targetUserId });

        // Case 2 — xác nhận cho người thao tác
        if (currentUserId > 0)
        {
            await _notificationService.NotifyUsersAsync(new NotifyRequest
            {
                Category = NotificationCategories.Permission,
                Severity = "Info",
                Title = "Đã thu hồi ủy quyền vai trò",
                Body = $"Bạn vừa thu hồi ủy quyền vai trò '{roleName}' của user '{targetUsername}'.",
                ActionUrl = "/permission-delegation",
                SourceEntityName = "UserRoles",
                SourceEntityId = targetUserId.ToString(),
                TriggeredByUserId = currentUserId
            }, new[] { currentUserId });
        }
    }

    /// <summary>Lỗi gửi thông báo không được làm hỏng nghiệp vụ ủy quyền — chỉ log cảnh báo.</summary>
    private async Task TryNotifyAsync(Func<Task> notify)
    {
        try
        {
            await notify();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Không gửi được notification cho sự kiện PERMISSION_DELEGATED");
        }
    }
}
