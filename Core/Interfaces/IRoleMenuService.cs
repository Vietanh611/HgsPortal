using Domain.Entities.System;
using Hgs.Share.Requests.RoleMenus;

namespace Core.Interfaces;

public interface IRoleMenuService
{
    Task<IEnumerable<RoleMenus>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<RoleMenus?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<IEnumerable<RoleMenus>> GetByRoleIdAsync(int roleId, CancellationToken cancellationToken = default);
    Task<IEnumerable<RoleMenus>> GetByMenuIdAsync(int menuId, CancellationToken cancellationToken = default);
    Task<RoleMenus> CreateAsync(RoleMenusCreateRequest request, int assignedBy, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);
    Task AssignMultipleMenusAsync(int roleId, List<int> menuIds, int assignedBy, CancellationToken cancellationToken = default);
    Task RemoveMultipleMenusAsync(int roleId, List<int> menuIds, CancellationToken cancellationToken = default);
}
