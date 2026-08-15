using Core.Interfaces;
using Data.DbContexts;
using Domain.Entities.Identity;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Core.Services;

public class OrgScopeService : IOrgScopeService
{
    private readonly HgsDbContext _dbContext;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IMenuService _menuService;

    public OrgScopeService(
        HgsDbContext dbContext,
        IHttpContextAccessor httpContextAccessor,
        IMenuService menuService)
    {
        _dbContext = dbContext;
        _httpContextAccessor = httpContextAccessor;
        _menuService = menuService;
    }

    private int GetCurrentUserId()
    {
        var claim = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(claim, out var userId) ? userId : 0;
    }

    private async Task<bool> IsCurrentUserSuperAdminAsync(CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        return userId > 0 && await _menuService.IsSuperAdminAsync(userId, cancellationToken);
    }

    /// <summary>
    /// Paths của các org unit mà caller quản lý.
    /// Anchor = chính User.OrganizationUnitId của caller (org + cấp con).
    /// SUPER_ADMIN → null (tất cả). User chưa gắn org → rỗng.
    /// </summary>
    public async Task<List<string>?> GetCallerScopePathsAsync(CancellationToken cancellationToken = default)
    {
        if (await IsCurrentUserSuperAdminAsync(cancellationToken))
        {
            return null;
        }

        var userId = GetCurrentUserId();
        if (userId <= 0)
        {
            return new List<string>();
        }

        var userOrgPath = await _dbContext.Users
            .AsNoTracking()
            .Where(u => u.Id == userId)
            .Select(u => u.OrganizationUnit != null ? u.OrganizationUnit.Path : null)
            .FirstOrDefaultAsync(cancellationToken);

        if (string.IsNullOrEmpty(userOrgPath))
        {
            return new List<string>();
        }

        return new List<string> { userOrgPath };
    }

    public async Task<bool> IsOrgUnitInScopeAsync(int orgUnitId, CancellationToken cancellationToken = default)
    {
        var scopePaths = await GetCallerScopePathsAsync(cancellationToken);
        if (scopePaths is null)
        {
            return true;
        }

        if (!scopePaths.Any())
        {
            return false;
        }

        var orgPath = await _dbContext.OrganizationUnits
            .AsNoTracking()
            .Where(ou => ou.Id == orgUnitId)
            .Select(ou => ou.Path)
            .FirstOrDefaultAsync(cancellationToken);

        if (string.IsNullOrEmpty(orgPath))
        {
            return false;
        }

        return scopePaths.Any(path => orgPath == path || orgPath.StartsWith(path + "/"));
    }

    public async Task<bool> IsUserInScopeAsync(int targetUserId, CancellationToken cancellationToken = default)
    {
        if (targetUserId == GetCurrentUserId())
        {
            return true;
        }

        var scopePaths = await GetCallerScopePathsAsync(cancellationToken);
        if (scopePaths is null)
        {
            return true;
        }

        if (!scopePaths.Any())
        {
            return false;
        }

        var orgPath = await _dbContext.Users
            .AsNoTracking()
            .Where(u => u.Id == targetUserId && !u.IsDeleted)
            .Select(u => u.OrganizationUnit != null ? u.OrganizationUnit.Path : null)
            .FirstOrDefaultAsync(cancellationToken);

        if (string.IsNullOrEmpty(orgPath))
        {
            return false;
        }

        return scopePaths.Any(path => orgPath == path || orgPath.StartsWith(path + "/"));
    }

    public async Task<bool> IsRoleAssignableAsync(int roleId, CancellationToken cancellationToken = default)
    {
        var role = await _dbContext.Roles
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == roleId, cancellationToken);

        if (role is null || !role.IsActive || role.IsSystemRole)
        {
            return false;
        }

        if (await IsCurrentUserSuperAdminAsync(cancellationToken))
        {
            return true;
        }

        if (!role.OrganizationUnitId.HasValue)
        {
            return false;
        }

        return await IsOrgUnitInScopeAsync(role.OrganizationUnitId.Value, cancellationToken);
    }

    public async Task<IEnumerable<Roles>> GetAssignableRolesAsync(CancellationToken cancellationToken = default)
    {
        var scopePaths = await GetCallerScopePathsAsync(cancellationToken);
        if (scopePaths is null)
        {
            return await _dbContext.Roles
                .AsNoTracking()
                .Include(r => r.OrganizationUnit)
                .Where(r => r.IsActive && !r.IsSystemRole)
                .OrderBy(r => r.Name)
                .ToListAsync(cancellationToken);
        }

        if (!scopePaths.Any())
        {
            return Enumerable.Empty<Roles>();
        }

        var roleOrgs = await _dbContext.OrganizationUnits
            .AsNoTracking()
            .Where(ou => scopePaths.Any(path => ou.Path == path || (ou.Path != null && ou.Path.StartsWith(path + "/"))))
            .Select(ou => ou.Id)
            .ToListAsync(cancellationToken);

        return await _dbContext.Roles
            .AsNoTracking()
            .Include(r => r.OrganizationUnit)
            .Where(r => r.IsActive && !r.IsSystemRole && r.OrganizationUnitId.HasValue && roleOrgs.Contains(r.OrganizationUnitId.Value))
            .OrderBy(r => r.Name)
            .ToListAsync(cancellationToken);
    }
}
