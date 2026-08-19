using Core.Interfaces.Identity;
using Core.Interfaces.Operations;
using Data.DbContexts;
using Domain.Entities.System;
using Hgs.Share.Dtos;
using Hgs.Share.Requests.UserMenus;
using Hgs.Share.Responses.Menus;
using Hgs.Share.Responses.UserMenus;
using Microsoft.EntityFrameworkCore;

namespace Core.Services.Identity;

public class UserMenuService : IUserMenuService
{
    private readonly HgsDbContext _dbContext;
    private readonly IAuditLogService _auditLog;
    private readonly ICacheService _cacheService;
    private readonly IOrgScopeService _orgScope;

    public UserMenuService(HgsDbContext dbContext, IAuditLogService auditLog, ICacheService cacheService, IOrgScopeService orgScope)
    {
        _dbContext = dbContext;
        _auditLog = auditLog;
        _cacheService = cacheService;
        _orgScope = orgScope;
    }

    public async Task<IEnumerable<UserMenuDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var userMenus = await _dbContext.UserMenus
            .Include(x => x.Menu)
                .ThenInclude(m => m.Children)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        return userMenus.Select(MapToDto);
    }

    public async Task<IEnumerable<MenusGetByUserIdResponse>> GetByUserIdAsync(int userId, CancellationToken cancellationToken = default)
    {
        // Get user-specific menu assignments
        var userMenuIds = await _dbContext.UserMenus
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .Select(x => x.MenuId)
            .ToListAsync(cancellationToken);

        // Get user's roles
        var userRoleIds = await _dbContext.UserRoles
            .AsNoTracking()
            .Where(ur => ur.UserId == userId)
            .Select(ur => ur.RoleId)
            .ToListAsync(cancellationToken);

        // Get all menus from user's roles
        var roleMenuIds = await _dbContext.RoleMenus
            .AsNoTracking()
            .Where(rm => userRoleIds.Contains(rm.RoleId))
            .Select(rm => rm.MenuId)
            .ToListAsync(cancellationToken);

        // Union of role menus and user menus
        var allAccessibleMenuIds = userMenuIds.Union(roleMenuIds).ToList();

        if (!allAccessibleMenuIds.Any())
            return [];

        var allMenus = await _dbContext.Menus
            .AsNoTracking()
            .OrderBy(x => x.SortOrder)
            .ToListAsync(cancellationToken);

        var menuLookup = allMenus.ToDictionary(x => x.Id);

        var visibleMenuIds = new HashSet<int>(allAccessibleMenuIds);

        // Include parent menus for hierarchy
        foreach (var menuId in allAccessibleMenuIds)
        {
            if (!menuLookup.TryGetValue(menuId, out var menu))
                continue;

            var parentId = menu.ParentId;

            while (parentId.HasValue)
            {
                if (!visibleMenuIds.Add(parentId.Value))
                    break;

                if (!menuLookup.TryGetValue(parentId.Value, out var parent))
                    break;

                parentId = parent.ParentId;
            }
        }

        var visibleMenus = allMenus
            .Where(x => visibleMenuIds.Contains(x.Id))
            .ToList();

        var rootMenus = visibleMenus
            .Where(x => !x.ParentId.HasValue ||
                        !visibleMenuIds.Contains(x.ParentId.Value))
            .OrderBy(x => x.SortOrder)
            .ToList();

        return BuildMenuHierarchy(rootMenus, visibleMenus);
    }

    public async Task<IEnumerable<int>> GetMenuIdsByUserIdAsync(int userId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.UserMenus
            .Where(x => x.UserId == userId)
            .Select(x => x.MenuId)
            .ToListAsync(cancellationToken);
    }

    public async Task<UserMenuAssignmentDetailsResponse> GetAssignmentDetailsByUserIdAsync(int userId, CancellationToken cancellationToken = default)
    {
        var userRoleIds = await _dbContext.UserRoles
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .Select(x => x.RoleId)
            .ToListAsync(cancellationToken);

        var roleMenuIds = await _dbContext.RoleMenus
            .AsNoTracking()
            .Where(rm => userRoleIds.Contains(rm.RoleId))
            .Select(rm => rm.MenuId)
            .ToListAsync(cancellationToken);

        var userMenuIds = await _dbContext.UserMenus
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .Select(x => x.MenuId)
            .ToListAsync(cancellationToken);

        return new UserMenuAssignmentDetailsResponse
        {
            RoleMenuIds = roleMenuIds.Distinct().OrderBy(x => x).ToList(),
            UserMenuIds = userMenuIds.Distinct().OrderBy(x => x).ToList()
        };
    }

    private static List<MenusGetByUserIdResponse> BuildMenuHierarchy(
        List<Menus> currentMenus,
        List<Menus> allMenus)
    {
        return currentMenus
            .OrderBy(x => x.SortOrder)
            .Select(menu =>
            {
                var dto = MapToGetByUserIdResponse(menu);

                var children = allMenus
                    .Where(x => x.ParentId == menu.Id)
                    .OrderBy(x => x.SortOrder)
                    .ToList();

                dto.Children = BuildMenuHierarchy(children, allMenus);

                return dto;
            })
            .ToList();
    }

    private static MenusGetByUserIdResponse MapToGetByUserIdResponse(Menus menu) => new()
    {
        Id = menu.Id,
        ParentId = menu.ParentId,
        Code = menu.Code,
        Name = menu.Name,
        Route = menu.Route,
        Component = menu.Component,
        Icon = menu.Icon,
        SortOrder = menu.SortOrder,
        IsVisible = menu.IsVisible,
        IsActive = menu.IsActive
    };

    private UserMenuDto MapToDto(UserMenus userMenu)
    {
        return new UserMenuDto
        {
            Id = userMenu.Id,
            UserId = userMenu.UserId,
            MenuId = userMenu.MenuId,
            AssignedAt = userMenu.AssignedAt,
            AssignedBy = userMenu.AssignedBy,
            Menu = MapMenuToDto(userMenu.Menu)
        };
    }

    private MenuDto MapMenuToDto(Menus menu)
    {
        return new MenuDto
        {
            Id = menu.Id,
            ParentId = menu.ParentId,
            Code = menu.Code,
            Name = menu.Name,
            Route = menu.Route,
            Component = menu.Component,
            Icon = menu.Icon,
            SortOrder = menu.SortOrder,
            IsVisible = menu.IsVisible,
            IsActive = menu.IsActive,
            Children = menu.Children.Select(MapMenuToDto).ToList()
        };
    }

    public async Task<UserMenus> CreateAsync(UserMenusCreateRequest request, int assignedBy, CancellationToken cancellationToken = default)
    {
        if (!await _orgScope.IsUserInScopeAsync(request.UserId, cancellationToken))
        {
            throw new UnauthorizedAccessException("Bạn không có quyền thực hiện thao tác này");
        }

        var existing = await _dbContext.UserMenus
            .AnyAsync(x => x.UserId == request.UserId && x.MenuId == request.MenuId, cancellationToken);
        if (existing)
        {
            throw new InvalidOperationException("User already has this menu assigned");
        }

        var userMenu = new UserMenus
        {
            UserId = request.UserId,
            MenuId = request.MenuId,
            AssignedAt = DateTime.UtcNow,
            AssignedBy = assignedBy
        };

        _dbContext.UserMenus.Add(userMenu);
        await _dbContext.SaveChangesAsync(cancellationToken);

        var user = await _dbContext.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);
        var menu = await _dbContext.Menus.AsNoTracking().FirstOrDefaultAsync(m => m.Id == request.MenuId, cancellationToken);

        await _auditLog.LogSecurityEventAsync(
            action: "MENU_ASSIGNED_TO_USER",
            eventCategory: "Permission", success: true, severity: "Warning",
            userId: assignedBy,
            targetUserId: request.UserId,
            entityName: "Menus",
            entityId: request.MenuId,
            detail: $"Gán menu '{menu?.Name}' cho user '{user?.Username}'",
            newValue: new { request.UserId, menuId = request.MenuId, menu?.Name });

        await _cacheService.ClearAllMenuCacheAsync(cancellationToken);
        return userMenu;
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var userMenu = await _dbContext.UserMenus
            .Include(x => x.Menu)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (userMenu is null)
        {
            return false;
        }

        _dbContext.UserMenus.Remove(userMenu);
        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditLog.LogSecurityEventAsync(
            action: "MENU_REVOKED_FROM_USER",
            eventCategory: "Permission", success: true, severity: "Warning",
            targetUserId: userMenu.UserId,
            entityName: "Menus",
            entityId: userMenu.MenuId,
            detail: $"Gỡ menu '{userMenu.Menu?.Name}' khỏi user #{userMenu.UserId}",
            oldValue: new { userMenu.UserId, menuId = userMenu.MenuId, MenuName = userMenu.Menu?.Name });

        await _cacheService.ClearAllMenuCacheAsync(cancellationToken);
        return true;
    }

    public async Task<bool> AssignMultipleMenusAsync(int userId, List<int> menuIds, int assignedBy, CancellationToken cancellationToken = default)
    {
        if (!await _orgScope.IsUserInScopeAsync(userId, cancellationToken))
        {
            throw new UnauthorizedAccessException("Bạn không có quyền thực hiện thao tác này");
        }

        var existingMenus = await _dbContext.UserMenus
            .Where(x => x.UserId == userId)
            .Select(x => x.MenuId)
            .ToListAsync(cancellationToken);

        var newMenuIds = menuIds.Except(existingMenus).ToList();

        var createdUserMenus = new List<UserMenus>();
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
            createdUserMenus.Add(userMenu);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        var user = await _dbContext.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

        // 1 dòng audit cho mỗi menu được gán
        foreach (var userMenu in createdUserMenus)
        {
            await _auditLog.LogSecurityEventAsync(
                action: "MENU_ASSIGNED_TO_USER",
                eventCategory: "Permission", success: true, severity: "Warning",
                userId: assignedBy,
                targetUserId: userId,
                entityName: "Menus",
                entityId: userMenu.MenuId,
                detail: $"Gán menu #{userMenu.MenuId} cho user '{user?.Username}'",
                newValue: new { userId, menuId = userMenu.MenuId });
        }

        await _cacheService.ClearAllMenuCacheAsync(cancellationToken);
        return true;
    }

    public async Task<bool> RemoveMultipleMenusAsync(int userId, List<int> menuIds, CancellationToken cancellationToken = default)
    {
        if (!await _orgScope.IsUserInScopeAsync(userId, cancellationToken))
        {
            throw new UnauthorizedAccessException("Bạn không có quyền thực hiện thao tác này");
        }

        var userMenus = await _dbContext.UserMenus
            .Include(x => x.Menu)
            .Where(x => x.UserId == userId && menuIds.Contains(x.MenuId))
            .ToListAsync(cancellationToken);

        _dbContext.UserMenus.RemoveRange(userMenus);
        await _dbContext.SaveChangesAsync(cancellationToken);

        // 1 dòng audit cho mỗi menu bị gỡ
        foreach (var userMenu in userMenus)
        {
            await _auditLog.LogSecurityEventAsync(
                action: "MENU_REVOKED_FROM_USER",
                eventCategory: "Permission", success: true, severity: "Warning",
                targetUserId: userId,
                entityName: "Menus",
                entityId: userMenu.MenuId,
                detail: $"Gỡ menu '{userMenu.Menu?.Name}' khỏi user #{userId}",
                oldValue: new { userId, menuId = userMenu.MenuId, MenuName = userMenu.Menu?.Name });
        }

        await _cacheService.ClearAllMenuCacheAsync(cancellationToken);
        return true;
    }
}
