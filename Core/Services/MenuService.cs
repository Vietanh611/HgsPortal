using Core.Interfaces;
using Data.DbContexts;
using Domain.Entities.System;
using Hgs.Share.Requests.Menus;
using Hgs.Share.Responses.Menus;
using Microsoft.EntityFrameworkCore;

namespace Core.Services;

public class MenuService : IMenuService
{
    private readonly HgsDbContext _dbContext;

    public MenuService(HgsDbContext dbContext)
    {
        _dbContext = dbContext;
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
            ModuleId = request.ModuleId,
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

        if (request.ModuleId.HasValue)
        {
            menu.ModuleId = request.ModuleId.Value;
        }

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
        await _dbContext.SaveChangesAsync(cancellationToken);
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

        _dbContext.Menus.Remove(menu);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<List<MenusGetByUserIdResponse>> GetMenusByUserIdAsync(
    int userId,
    CancellationToken cancellationToken = default)
    {
        // Menu được gán trực tiếp cho user
        var userMenuIds = await _dbContext.UserMenus
            .Where(x => x.UserId == userId)
            .Select(x => x.MenuId)
            .ToListAsync(cancellationToken);

        if (!userMenuIds.Any())
            return [];

        // Chỉ query DB 1 lần
        var allMenus = await _dbContext.Menus
            .AsNoTracking()
            .OrderBy(x => x.SortOrder)
            .ToListAsync(cancellationToken);

        var menuLookup = allMenus.ToDictionary(x => x.Id);

        // Tập menu được phép hiển thị
        var visibleMenuIds = new HashSet<int>(userMenuIds);

        // Thêm toàn bộ menu cha
        foreach (var menuId in userMenuIds)
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

        return BuildMenuHierarchy(rootMenus, visibleMenus);
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
        ModuleId = menu.ModuleId,
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
