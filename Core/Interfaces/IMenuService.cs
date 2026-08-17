using Domain.Entities.System;
using Hgs.Share.Requests.Menus;
using Hgs.Share.Responses.Menus;

namespace Core.Interfaces;

public interface IMenuService
{
    /// <summary>Trả về menu dưới dạng cây phân cấp theo ParentId/Children (khác GetAllFlatAsync trả danh sách phẳng).</summary>
    Task<IEnumerable<Menus>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<Menus>> GetAllFlatAsync(CancellationToken cancellationToken = default);
    Task<Menus?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    /// <summary>Tạo menu mới; kiểm tra Code duy nhất. Vì cấu trúc menu ảnh hưởng tới mọi user, toàn bộ cache menu được xóa.</summary>
    Task<Menus> CreateAsync(MenusCreateRequest request, CancellationToken cancellationToken = default);
    /// <summary>Cập nhật menu; xóa toàn bộ cache menu vì thay đổi cấu trúc menu ảnh hưởng tới cây menu của mọi user.</summary>
    Task<Menus?> UpdateAsync(int id, MenusUpdateRequest request, CancellationToken cancellationToken = default);
    /// <summary>Xóa menu; xóa toàn bộ cache menu vì mọi user có thể đang tham chiếu menu này.</summary>
    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);
    /// <summary>Trả về cây menu user được phép: hợp nhất menu gán trực tiếp (UserMenus) và menu kế thừa từ role (RoleMenus), tự bổ sung các menu cha để dựng cây. SUPER_ADMIN nhận toàn bộ menu. Kết quả cache 5 phút.</summary>
    Task<List<MenusGetByUserIdResponse>> GetMenusByUserIdAsync(int userId, CancellationToken cancellationToken = default);
    /// <summary>Tập mã menu hiệu dụng của user (gán trực tiếp + kế thừa từ role), so khớp không phân biệt hoa thường — dùng cho kiểm tra quyền truy cập menu. Được cache.</summary>
    Task<HashSet<string>> GetEffectiveMenuCodesAsync(int userId, CancellationToken cancellationToken = default);
    /// <summary>Xác định user có đang giữ role SuperAdmin hoạt động hay không — quyết định bypass phạm vi tổ chức ở nhiều service. Được cache.</summary>
    Task<bool> IsSuperAdminAsync(int userId, CancellationToken cancellationToken = default);
}
