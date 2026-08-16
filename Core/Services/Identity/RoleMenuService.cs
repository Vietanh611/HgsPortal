using Core.Interfaces;
using Data.DbContexts;
using Domain.Entities.System;
using Hgs.Share.Requests.RoleMenus;
using Microsoft.EntityFrameworkCore;

namespace Core.Services.Identity;

public class RoleMenuService : IRoleMenuService
{
    private readonly HgsDbContext _dbContext;
    private readonly IAuditLogService _auditLog;
    private readonly ICacheService _cacheService;

    public RoleMenuService(HgsDbContext dbContext, IAuditLogService auditLog, ICacheService cacheService)
    {
        _dbContext = dbContext;
        _auditLog = auditLog;
        _cacheService = cacheService;
    }

    public async Task<IEnumerable<RoleMenus>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.RoleMenus
            .Include(rm => rm.Role)
            .Include(rm => rm.Menu)
            .AsNoTracking()
            .OrderByDescending(rm => rm.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<RoleMenus?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.RoleMenus
            .Include(rm => rm.Role)
            .Include(rm => rm.Menu)
            .AsNoTracking()
            .FirstOrDefaultAsync(rm => rm.Id == id, cancellationToken);
    }

    public async Task<IEnumerable<RoleMenus>> GetByRoleIdAsync(int roleId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.RoleMenus
            .Include(rm => rm.Role)
            .Include(rm => rm.Menu)
            .Where(rm => rm.RoleId == roleId)
            .AsNoTracking()
            .OrderByDescending(rm => rm.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<RoleMenus>> GetByMenuIdAsync(int menuId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.RoleMenus
            .Include(rm => rm.Role)
            .Include(rm => rm.Menu)
            .Where(rm => rm.MenuId == menuId)
            .AsNoTracking()
            .OrderByDescending(rm => rm.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<RoleMenus> CreateAsync(RoleMenusCreateRequest request, int assignedBy, CancellationToken cancellationToken = default)
    {
        // Check if role exists
        var role = await _dbContext.Roles
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == request.RoleId, cancellationToken);
        if (role is null)
        {
            throw new KeyNotFoundException($"Role with ID {request.RoleId} not found");
        }

        // Check if menu exists
        var menu = await _dbContext.Menus
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.Id == request.MenuId, cancellationToken);
        if (menu is null)
        {
            throw new KeyNotFoundException($"Menu with ID {request.MenuId} not found");
        }

        // Check if role already has this menu
        var existingAssignment = await _dbContext.RoleMenus
            .AsNoTracking()
            .FirstOrDefaultAsync(rm => rm.RoleId == request.RoleId && rm.MenuId == request.MenuId, cancellationToken);
        if (existingAssignment is not null)
        {
            throw new InvalidOperationException($"Role already has menu {menu.Name} assigned");
        }

        var roleMenu = new RoleMenus
        {
            RoleId = request.RoleId,
            MenuId = request.MenuId,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = assignedBy,
        };

        _dbContext.RoleMenus.Add(roleMenu);
        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditLog.LogSecurityEventAsync(
            action: "MENU_ASSIGNED_TO_ROLE",
            eventCategory: "Permission", success: true, severity: "Warning",
            userId: assignedBy,
            entityName: "Roles",
            entityId: request.RoleId,
            detail: $"Gán menu '{menu.Name}' cho role '{role.Name}'",
            newValue: new { roleId = request.RoleId, menuId = request.MenuId, menu.Name });

        await _cacheService.ClearAllMenuCacheAsync(cancellationToken);
        return roleMenu;
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var roleMenu = await _dbContext.RoleMenus
            .Include(rm => rm.Role)
            .Include(rm => rm.Menu)
            .FirstOrDefaultAsync(rm => rm.Id == id, cancellationToken);

        if (roleMenu is null)
        {
            return false;
        }

        _dbContext.RoleMenus.Remove(roleMenu);
        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditLog.LogSecurityEventAsync(
            action: "MENU_REVOKED_FROM_ROLE",
            eventCategory: "Permission", success: true, severity: "Warning",
            entityName: "Roles",
            entityId: roleMenu.RoleId,
            detail: $"Gỡ menu '{roleMenu.Menu?.Name}' khỏi role '{roleMenu.Role?.Name}'",
            oldValue: new { roleMenu.RoleId, menuId = roleMenu.MenuId, MenuName = roleMenu.Menu?.Name });

        await _cacheService.ClearAllMenuCacheAsync(cancellationToken);
        return true;
    }

    public async Task AssignMultipleMenusAsync(int roleId, List<int> menuIds, int assignedBy, CancellationToken cancellationToken = default)
    {
        // Check if role exists
        var role = await _dbContext.Roles
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == roleId, cancellationToken);
        if (role is null)
        {
            throw new KeyNotFoundException($"Role with ID {roleId} not found");
        }

        // Get existing menu assignments
        var existingMenuIds = await _dbContext.RoleMenus
            .Where(rm => rm.RoleId == roleId)
            .Select(rm => rm.MenuId)
            .ToListAsync(cancellationToken);

        // Filter out already assigned menus
        var newMenuIds = menuIds.Except(existingMenuIds).ToList();

        var createdRoleMenus = new List<RoleMenus>();
        foreach (var menuId in newMenuIds)
        {
            // Check if menu exists
            var menu = await _dbContext.Menus
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.Id == menuId, cancellationToken);
            if (menu is null)
            {
                throw new KeyNotFoundException($"Menu with ID {menuId} not found");
            }

            var roleMenu = new RoleMenus
            {
                RoleId = roleId,
                MenuId = menuId,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = assignedBy,
            };

            _dbContext.RoleMenus.Add(roleMenu);
            createdRoleMenus.Add(roleMenu);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        // 1 dòng audit cho mỗi menu được gán
        foreach (var roleMenu in createdRoleMenus)
        {
            await _auditLog.LogSecurityEventAsync(
                action: "MENU_ASSIGNED_TO_ROLE",
                eventCategory: "Permission", success: true, severity: "Warning",
                userId: assignedBy,
                entityName: "Roles",
                entityId: roleId,
                detail: $"Gán menu #{roleMenu.MenuId} cho role '{role.Name}'",
                newValue: new { roleId, menuId = roleMenu.MenuId });
        }

        await _cacheService.ClearAllMenuCacheAsync(cancellationToken);
    }

    public async Task RemoveMultipleMenusAsync(int roleId, List<int> menuIds, CancellationToken cancellationToken = default)
    {
        // Check if role exists
        var role = await _dbContext.Roles
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == roleId, cancellationToken);
        if (role is null)
        {
            throw new KeyNotFoundException($"Role with ID {roleId} not found");
        }

        var roleMenus = await _dbContext.RoleMenus
            .Include(rm => rm.Menu)
            .Where(rm => rm.RoleId == roleId && menuIds.Contains(rm.MenuId))
            .ToListAsync(cancellationToken);

        _dbContext.RoleMenus.RemoveRange(roleMenus);
        await _dbContext.SaveChangesAsync(cancellationToken);

        // 1 dòng audit cho mỗi menu bị gỡ
        foreach (var roleMenu in roleMenus)
        {
            await _auditLog.LogSecurityEventAsync(
                action: "MENU_REVOKED_FROM_ROLE",
                eventCategory: "Permission", success: true, severity: "Warning",
                entityName: "Roles",
                entityId: roleId,
                detail: $"Gỡ menu '{roleMenu.Menu?.Name}' khỏi role '{role.Name}'",
                oldValue: new { roleId, menuId = roleMenu.MenuId, MenuName = roleMenu.Menu?.Name });
        }

        await _cacheService.ClearAllMenuCacheAsync(cancellationToken);
    }
}
