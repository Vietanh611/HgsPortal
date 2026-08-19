using Core.Constants;
using Core.Interfaces.Identity;
using Core.Interfaces.Notifications;
using Core.Interfaces.Operations;
using Data.DbContexts;
using Domain.Entities.Identity;
using Hgs.Share.Requests.Notifications;
using Hgs.Share.Requests.UserRoles;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Core.Services.Identity;

public class UserRoleService : IUserRoleService
{
    private readonly HgsDbContext _dbContext;
    private readonly IAuditLogService _auditLog;
    private readonly INotificationService _notificationService;
    private readonly ICacheService _cacheService;
    private readonly IOrgScopeService _orgScope;
    private readonly ILogger<UserRoleService> _logger;

    public UserRoleService(HgsDbContext dbContext, IAuditLogService auditLog, INotificationService notificationService, ICacheService cacheService, IOrgScopeService orgScope, ILogger<UserRoleService> logger)
    {
        _dbContext = dbContext;
        _auditLog = auditLog;
        _notificationService = notificationService;
        _cacheService = cacheService;
        _orgScope = orgScope;
        _logger = logger;
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

        // LogSecurityEventAsync tự SaveChanges — thay thế Log CRUD cũ (không thêm dòng trùng)
        await _auditLog.LogSecurityEventAsync(
            action: "ROLE_ASSIGNED",
            eventCategory: "Permission", success: true, severity: "Warning",
            userId: assignedBy,             // người thực hiện
            targetUserId: request.UserId,   // người được gán role
            entityName: "Roles",
            entityId: request.RoleId,       // EntityId là int? — truyền thẳng
            detail: $"Gán role '{role.Name}' cho user '{user.Username}'",
            newValue: new { role.Id, role.Name, targetUserId = user.Id });

        await TryNotifyAsync(() => NotifyRoleAssignedAsync(user.Username, role.Name, user.Id));

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
            .Include(ur => ur.Role)
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

        _dbContext.UserRoles.Remove(userRole);
        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditLog.LogSecurityEventAsync(
            action: "ROLE_REVOKED",
            eventCategory: "Permission", success: true, severity: "Warning",
            targetUserId: userRole.UserId,
            entityName: "Roles",
            entityId: userRole.RoleId,
            detail: $"Gỡ role '{userRole.Role?.Name}' khỏi user '{userRole.User?.Username}'",
            oldValue: new { userRole.RoleId, RoleName = userRole.Role?.Name, userRole.UserId });

        if (userRole.User is not null)
        {
            await TryNotifyAsync(() => NotifyRoleRevokedAsync(userRole.User.Username, userRole.Role?.Name, userRole.UserId));
        }

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
        var createdRoleNames = new List<string>();
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
            createdRoleNames.Add(role.Name);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        // 1 dòng audit cho mỗi role được gán — truy vết theo TargetUserId/EntityId không bỏ sót
        foreach (var userRole in createdUserRoles)
        {
            await _auditLog.LogSecurityEventAsync(
                action: "ROLE_ASSIGNED",
                eventCategory: "Permission", success: true, severity: "Warning",
                userId: assignedBy,
                targetUserId: userId,
                entityName: "Roles",
                entityId: userRole.RoleId,
                detail: $"Gán role #{userRole.RoleId} cho user '{user.Username}'",
                newValue: new { roleId = userRole.RoleId, userId });
        }

        if (createdRoleNames.Count > 0)
        {
            await TryNotifyAsync(() => NotifyRolesAssignedAsync(user.Username, createdRoleNames, userId));
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
            .Include(ur => ur.Role)
            .Where(ur => ur.UserId == userId && roleIds.Contains(ur.RoleId))
            .ToListAsync(cancellationToken);

        // Check if removing would leave user with no roles
        if (totalRoleCount <= rolesToRemove.Count)
        {
            throw new InvalidOperationException("Cannot remove the last role from a user");
        }

        _dbContext.UserRoles.RemoveRange(rolesToRemove);
        await _dbContext.SaveChangesAsync(cancellationToken);

        // 1 dòng audit cho mỗi role bị gỡ
        foreach (var userRole in rolesToRemove)
        {
            await _auditLog.LogSecurityEventAsync(
                action: "ROLE_REVOKED",
                eventCategory: "Permission", success: true, severity: "Warning",
                targetUserId: userId,
                entityName: "Roles",
                entityId: userRole.RoleId,
                detail: $"Gỡ role '{userRole.Role?.Name}' khỏi user '{user.Username}'",
                oldValue: new { userRole.RoleId, RoleName = userRole.Role?.Name, userId });
        }

        if (rolesToRemove.Count > 0)
        {
            var removedRoleNames = rolesToRemove.Select(r => r.Role?.Name).Where(n => !string.IsNullOrEmpty(n)).Cast<string>().ToList();
            await TryNotifyAsync(() => NotifyRolesRevokedAsync(user.Username, removedRoleNames, userId));
        }

        await _cacheService.ClearAllMenuCacheAsync(cancellationToken);
    }

    private async Task NotifyRoleAssignedAsync(string username, string roleName, int userId)
    {
        await _notificationService.NotifyUsersAsync(new NotifyRequest
        {
            Category = NotificationCategories.Permission,
            Severity = "Info",
            Title = "Bạn được gán vai trò mới",
            Body = $"Bạn vừa được gán vai trò '{roleName}'.",
            ActionUrl = "/profile",
            SourceEntityName = "UserRoles",
            SourceEntityId = userId.ToString()
        }, new[] { userId });
    }

    private async Task NotifyRolesAssignedAsync(string username, IEnumerable<string> roleNames, int userId)
    {
        await _notificationService.NotifyUsersAsync(new NotifyRequest
        {
            Category = NotificationCategories.Permission,
            Severity = "Info",
            Title = "Bạn được gán vai trò mới",
            Body = $"Bạn vừa được gán các vai trò: {string.Join(", ", roleNames)}.",
            ActionUrl = "/profile",
            SourceEntityName = "UserRoles",
            SourceEntityId = userId.ToString()
        }, new[] { userId });
    }

    private async Task NotifyRoleRevokedAsync(string username, string? roleName, int userId)
    {
        await _notificationService.NotifyUsersAsync(new NotifyRequest
        {
            Category = NotificationCategories.Permission,
            Severity = "Info",
            Title = "Vai trò của bạn bị thu hồi",
            Body = $"Vai trò '{roleName}' của bạn vừa bị thu hồi.",
            ActionUrl = "/profile",
            SourceEntityName = "UserRoles",
            SourceEntityId = userId.ToString()
        }, new[] { userId });
    }

    private async Task NotifyRolesRevokedAsync(string username, IEnumerable<string> roleNames, int userId)
    {
        await _notificationService.NotifyUsersAsync(new NotifyRequest
        {
            Category = NotificationCategories.Permission,
            Severity = "Info",
            Title = "Vai trò của bạn bị thu hồi",
            Body = $"Các vai trò của bạn vừa bị thu hồi: {string.Join(", ", roleNames)}.",
            ActionUrl = "/profile",
            SourceEntityName = "UserRoles",
            SourceEntityId = userId.ToString()
        }, new[] { userId });
    }

    /// <summary>Lỗi gửi thông báo không được làm hỏng nghiệp vụ gán/gỡ role — chỉ log cảnh báo.</summary>
    private async Task TryNotifyAsync(Func<Task> notify)
    {
        try
        {
            await notify();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Không gửi được notification cho sự kiện ROLE_ASSIGNED/ROLE_REVOKED");
        }
    }
}
