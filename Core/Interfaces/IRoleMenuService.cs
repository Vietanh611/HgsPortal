using Domain.Entities.System;
using Hgs.Share.Requests.RoleMenus;

namespace Core.Interfaces;

public interface IRoleMenuService
{
    Task<IEnumerable<RoleMenus>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<RoleMenus?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<IEnumerable<RoleMenus>> GetByRoleIdAsync(int roleId, CancellationToken cancellationToken = default);
    Task<IEnumerable<RoleMenus>> GetByMenuIdAsync(int menuId, CancellationToken cancellationToken = default);
    /// <summary>Gán menu cho role; chống gán trùng. Vì mọi user mang role này đều kế thừa menu, toàn bộ cache menu được xóa để phản ánh quyền mới.</summary>
    Task<RoleMenus> CreateAsync(RoleMenusCreateRequest request, int assignedBy, CancellationToken cancellationToken = default);
    /// <summary>Gỡ menu khỏi role — user mang role này sẽ mất menu tương ứng; xóa toàn bộ cache menu.</summary>
    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);
    /// <summary>Gán nhiều menu cho role; bỏ qua menu đã gán trước đó (idempotent). Xóa toàn bộ cache menu.</summary>
    Task AssignMultipleMenusAsync(int roleId, List<int> menuIds, int assignedBy, CancellationToken cancellationToken = default);
    /// <summary>Gỡ nhiều menu khỏi role; xóa toàn bộ cache menu.</summary>
    Task RemoveMultipleMenusAsync(int roleId, List<int> menuIds, CancellationToken cancellationToken = default);
}
