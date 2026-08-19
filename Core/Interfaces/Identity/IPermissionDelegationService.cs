using Hgs.Share.Requests.PermissionDelegation;
using Hgs.Share.Responses.PermissionDelegation;

namespace Core.Interfaces.Identity;

public interface IPermissionDelegationService
{
    /// <summary>Danh sách user caller có thể quản lý: user hoạt động, chưa bị xóa, thuộc phạm vi tổ chức của caller (org + cấp con), không gồm chính caller.</summary>
    Task<IEnumerable<ManageableUserResponse>> GetManageableUsersAsync(CancellationToken cancellationToken = default);
    /// <summary>Danh sách role caller được phép ủy quyền ("Chọn quyền gán"): chỉ những role caller đang giữ (active, không phải hệ thống) — không ủy quyền vượt quyền của mình; SUPER_ADMIN nhận toàn bộ role active không phải hệ thống. Không giới hạn theo org.</summary>
    Task<IEnumerable<AssignableRoleResponse>> GetAssignableRolesAsync(CancellationToken cancellationToken = default);
    /// <summary>Ủy quyền role cho user khác, qua chuỗi kiểm tra: caller phải có menu PERMISSIONDELEGATION, không ủy quyền cho chính mình, user đích thuộc phạm vi tổ chức của caller, role phải là role caller đang giữ (active, không hệ thống; SUPER_ADMIN gán được mọi role); bỏ qua nếu user đã có role. Audit sự kiện Critical và xóa cache menu của user đích.</summary>
    Task AssignRoleAsync(AssignRoleRequest request, CancellationToken cancellationToken = default);
    /// <summary>Thu hồi ủy quyền role với chuỗi kiểm tra tương tự AssignRoleAsync (user đích phải thuộc phạm vi tổ chức, role phải là role caller đang giữ); bỏ qua nếu user đích không giữ role đó. Audit và xóa cache menu của user đích.</summary>
    Task RevokeRoleAsync(RevokeRoleRequest request, CancellationToken cancellationToken = default);
    /// <summary>Ủy quyền theo "tập đầy đủ": gán các role chưa có và thu hồi các role không còn trong danh sách (role hệ thống được giữ nguyên) — mô hình tick/bỏ trên giao diện. User đích phải thuộc phạm vi tổ chức của caller; role chỉ được chọn trong tập role caller đang giữ (SUPER_ADMIN nhận tất cả).</summary>
    Task AssignRolesAsync(AssignRolesRequest request, CancellationToken cancellationToken = default);
    /// <summary>Quyền hiệu dụng của một user: các role đang giữ và các menu nhận qua role (chỉ menu hoạt động) — dùng để xem lại ủy quyền. Trả null nếu user không tồn tại.</summary>
    Task<UserEffectivePermissionsResponse?> GetUserEffectivePermissionsAsync(int userId, CancellationToken cancellationToken = default);
}
