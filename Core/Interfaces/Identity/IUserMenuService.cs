using Domain.Entities.System;
using Hgs.Share.Dtos;
using Hgs.Share.Requests.UserMenus;
using Hgs.Share.Responses.Menus;
using Hgs.Share.Responses.UserMenus;

namespace Core.Interfaces.Identity;

public interface IUserMenuService
{
    Task<IEnumerable<UserMenuDto>> GetAllAsync(CancellationToken cancellationToken = default);
    /// <summary>Cây menu của user: hợp nhất menu gán trực tiếp (UserMenus) và menu kế thừa từ role; tự thêm menu cha để dựng cây hoàn chỉnh.</summary>
    Task<IEnumerable<MenusGetByUserIdResponse>> GetByUserIdAsync(int userId, CancellationToken cancellationToken = default);
    /// <summary>Chỉ trả về các menu gán TRỰC TIẾP (bảng UserMenus), không gồm menu kế thừa từ role — khác với GetByUserIdAsync (hợp nhất).</summary>
    Task<IEnumerable<int>> GetMenuIdsByUserIdAsync(int userId, CancellationToken cancellationToken = default);
    /// <summary>Tách riêng menu user nhận qua role (RoleMenuIds) và menu gán trực tiếp (UserMenuIds) của một user để phân biệt nguồn gốc từng menu.</summary>
    Task<UserMenuAssignmentDetailsResponse> GetAssignmentDetailsByUserIdAsync(int userId, CancellationToken cancellationToken = default);
    /// <summary>Gán menu trực tiếp cho user thuộc phạm vi tổ chức của caller; chống gán trùng. Xóa toàn bộ cache menu.</summary>
    Task<UserMenus> CreateAsync(UserMenusCreateRequest request, int assignedBy, CancellationToken cancellationToken = default);
    /// <summary>Gỡ menu gán trực tiếp; xóa toàn bộ cache menu.</summary>
    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);
    /// <summary>Gán nhiều menu trực tiếp; bỏ qua menu đã gán trước đó (idempotent). Caller phải thuộc phạm vi tổ chức của user. Xóa cache menu.</summary>
    Task<bool> AssignMultipleMenusAsync(int userId, List<int> menuIds, int assignedBy, CancellationToken cancellationToken = default);
    /// <summary>Gỡ nhiều menu gán trực tiếp; caller phải thuộc phạm vi tổ chức của user. Xóa cache menu.</summary>
    Task<bool> RemoveMultipleMenusAsync(int userId, List<int> menuIds, CancellationToken cancellationToken = default);
}
