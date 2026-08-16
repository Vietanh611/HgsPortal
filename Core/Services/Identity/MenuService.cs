using Core.Constants;
using Core.Interfaces;
using Data.DbContexts;
using Domain.Entities.System;
using Hgs.Share.Requests.Menus;
using Hgs.Share.Responses.Menus;
using Microsoft.EntityFrameworkCore;

namespace Core.Services.Identity;

public class MenuService : IMenuService
{
    private readonly HgsDbContext _dbContext;
    private readonly IAuditLogService _auditLog;
    private readonly ICacheService _cacheService;

    public MenuService(HgsDbContext dbContext, IAuditLogService auditLog, ICacheService cacheService)
    {
        _dbContext = dbContext;
        _auditLog = auditLog;
        _cacheService = cacheService;
    }

    public async Task<IEnumerable<Menus>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var allMenus = await _dbContext.Menus
            .AsNoTracking()
            .OrderBy(x => x.SortOrder)
            .ToListAsync(cancellationToken);

        var rootMenus = allMenus
            .Where(x => !x.ParentId.HasValue)
            .OrderBy(x => x.SortOrder)
            .ToList();

        return BuildMenuHierarchyForGetAll(rootMenus, allMenus);
    }

    public async Task<IEnumerable<Menus>> GetAllFlatAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.Menus
            .AsNoTracking()
            .OrderBy(x => x.SortOrder)
            .ToListAsync(cancellationToken);
    }

    private static List<Menus> BuildMenuHierarchyForGetAll(
        List<Menus> currentMenus,
        List<Menus> allMenus)
    {
        return currentMenus
            .OrderBy(x => x.SortOrder)
            .Select(menu =>
            {
                var children = allMenus
                    .Where(x => x.ParentId == menu.Id)
                    .OrderBy(x => x.SortOrder)
                    .ToList();

                menu.Children = BuildMenuHierarchyForGetAll(children, allMenus);

                return menu;
            })
            .ToList();
    }

    public async Task<Menus?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Menus
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<Menus> CreateAsync(MenusCreateRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Code))
        {
            throw new ArgumentException("Code is required");
        }

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            throw new ArgumentException("Name is required");
        }

        var exists = await _dbContext.Menus
            .AnyAsync(x => x.Code == request.Code, cancellationToken);
        if (exists)
        {
            throw new InvalidOperationException("Menu code already exists");
        }

        var menu = new Menus
        {
            ParentId = request.ParentId,
            Code = request.Code,
            Name = request.Name,
            Route = request.Route,
            Component = request.Component,
            Icon = request.Icon,
            SortOrder = request.SortOrder,
            IsVisible = request.IsVisible,
            IsActive = request.IsActive,
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.Menus.Add(menu);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _auditLog.Log(
            action: "CREATE",
            entityName: "Menus",
            entityId: menu.Id,
            oldValue: null,
            newValue: menu);

        await _dbContext.SaveChangesAsync(cancellationToken);

        // Clear all menu cache when a new menu is created
        await _cacheService.ClearAllMenuCacheAsync(cancellationToken);

        return menu;
    }

    public async Task<Menus?> UpdateAsync(int id, MenusUpdateRequest request, CancellationToken cancellationToken = default)
    {
        var menu = await _dbContext.Menus
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (menu is null)
        {
            return null;
        }

        var oldSnapshot = new
        {
            menu.Id,
            menu.ParentId,
            menu.Code,
            menu.Name,
            menu.Route,
            menu.Component,
            menu.Icon,
            menu.SortOrder,
            menu.IsVisible,
            menu.IsActive
        };

        if (request.ParentId.HasValue)
        {
            menu.ParentId = request.ParentId.Value;
        }

        if (!string.IsNullOrWhiteSpace(request.Code))
        {
            menu.Code = request.Code;
        }

        if (!string.IsNullOrWhiteSpace(request.Name))
        {
            menu.Name = request.Name;
        }

        if (request.Route is not null)
        {
            menu.Route = request.Route;
        }

        if (request.Component is not null)
        {
            menu.Component = request.Component;
        }

        if (request.Icon is not null)
        {
            menu.Icon = request.Icon;
        }

        if (request.SortOrder.HasValue)
        {
            menu.SortOrder = request.SortOrder.Value;
        }

        if (request.IsVisible.HasValue)
        {
            menu.IsVisible = request.IsVisible.Value;
        }

        if (request.IsActive.HasValue)
        {
            menu.IsActive = request.IsActive.Value;
        }

        menu.UpdatedAt = DateTime.UtcNow;

        _auditLog.Log(
            action: "UPDATE",
            entityName: "Menus",
            entityId: menu.Id,
            oldValue: oldSnapshot,
            newValue: menu);

        await _dbContext.SaveChangesAsync(cancellationToken);

        // Clear all menu cache when a menu is updated
        await _cacheService.ClearAllMenuCacheAsync(cancellationToken);

        return menu;
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var menu = await _dbContext.Menus
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (menu is null)
        {
            return false;
        }

        _auditLog.Log(
            action: "DELETE",
            entityName: "Menus",
            entityId: menu.Id,
            oldValue: menu,
            newValue: null);

        _dbContext.Menus.Remove(menu);
        await _dbContext.SaveChangesAsync(cancellationToken);

        // Clear all menu cache when a menu is deleted
        await _cacheService.ClearAllMenuCacheAsync(cancellationToken);

        return true;
    }

    public async Task<List<MenusGetByUserIdResponse>> GetMenusByUserIdAsync(
    int userId,
    CancellationToken cancellationToken = default)
    {
        if (await IsSuperAdminAsync(userId, cancellationToken))
        {
            var superAdminAllMenus = await _dbContext.Menus
                .AsNoTracking()
                .OrderBy(x => x.SortOrder)
                .ToListAsync(cancellationToken);

            var superAdminRootMenus = superAdminAllMenus
                .Where(x => !x.ParentId.HasValue)
                .OrderBy(x => x.SortOrder)
                .ToList();

            return BuildMenuHierarchy(superAdminRootMenus, superAdminAllMenus);
        }

        var cacheKey = $"menus:user:{userId}";

        // Try to get from cache first
        var cachedMenus = await _cacheService.GetAsync<List<MenusGetByUserIdResponse>>(cacheKey, cancellationToken);
        if (cachedMenus != null)
        {
            return cachedMenus;
        }

        // Cache miss - query from database
        // Menu user nhận từ role (kế thừa) + menu gán trực tiếp (tùy chỉnh riêng)
        var userMenuIds = await _dbContext.UserMenus
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .Select(x => x.MenuId)
            .ToListAsync(cancellationToken);

        var roleMenuIds = await _dbContext.UserRoles
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .SelectMany(x => x.Role.RoleMenus.Select(rm => rm.MenuId))
            .ToListAsync(cancellationToken);

        var allAccessibleMenuIds = userMenuIds.Union(roleMenuIds).ToList();

        if (!allAccessibleMenuIds.Any())
            return [];

        // Chỉ query DB 1 lần
        var allMenus = await _dbContext.Menus
            .AsNoTracking()
            .OrderBy(x => x.SortOrder)
            .ToListAsync(cancellationToken);

        var menuLookup = allMenus.ToDictionary(x => x.Id);

        // Tập menu được phép hiển thị
        var visibleMenuIds = new HashSet<int>(allAccessibleMenuIds);

        // Thêm toàn bộ menu cha
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

        // Chỉ lấy menu được phép
        var visibleMenus = allMenus
            .Where(x => visibleMenuIds.Contains(x.Id))
            .ToList();

        // Root menu
        var rootMenus = visibleMenus
            .Where(x => !x.ParentId.HasValue ||
                        !visibleMenuIds.Contains(x.ParentId.Value))
            .OrderBy(x => x.SortOrder)
            .ToList();

        var result = BuildMenuHierarchy(rootMenus, visibleMenus);

        // Cache the result for 5 minutes
        await _cacheService.SetAsync(cacheKey, result, TimeSpan.FromMinutes(5), cancellationToken);

        return result;
    }

    public async Task<HashSet<string>> GetEffectiveMenuCodesAsync(int userId, CancellationToken cancellationToken = default)
    {
        var cacheKey = $"menus:user:codes:{userId}";

        var cached = await _cacheService.GetAsync<HashSet<string>>(cacheKey, cancellationToken);
        if (cached != null)
        {
            return cached;
        }

        var directCodes = await _dbContext.UserMenus
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .Select(x => x.Menu.Code)
            .ToListAsync(cancellationToken);

        var roleCodes = await _dbContext.UserRoles
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .SelectMany(x => x.Role.RoleMenus.Select(rm => rm.Menu.Code))
            .ToListAsync(cancellationToken);

        var codes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        codes.UnionWith(directCodes);
        codes.UnionWith(roleCodes);

        await _cacheService.SetAsync(cacheKey, codes, cancellationToken: cancellationToken);

        return codes;
    }

    public async Task<bool> IsSuperAdminAsync(int userId, CancellationToken cancellationToken = default)
    {
        var cacheKey = $"users:superadmin:{userId}";

        var cached = await _cacheService.GetAsync<bool?>(cacheKey, cancellationToken);
        if (cached.HasValue)
        {
            return cached.Value;
        }

        var isSuperAdmin = await _dbContext.UserRoles
            .AsNoTracking()
            .AnyAsync(x => x.UserId == userId
                && x.Role.Code == RoleCodes.SuperAdmin
                && x.Role.IsActive, cancellationToken);

        await _cacheService.SetAsync(cacheKey, (bool?)isSuperAdmin, cancellationToken: cancellationToken);

        return isSuperAdmin;
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
}
