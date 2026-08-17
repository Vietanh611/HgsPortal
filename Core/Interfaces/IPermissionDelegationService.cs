using Hgs.Share.Requests.PermissionDelegation;
using Hgs.Share.Responses.PermissionDelegation;

namespace Core.Interfaces;

public interface IPermissionDelegationService
{
    /// <summary>Danh sách user caller có thể quản lý: user hoạt động, chưa bị xóa, thuộc phạm vi tổ chức của caller, không gồm chính caller.</summary>
    Task<IEnumerable<ManageableUserResponse>> GetManageableUsersAsync(CancellationToken cancellationToken = default);
    /// <summary>Danh sách role caller được phép ủy quyền: đang hoạt động, không phải role hệ thống, thuộc phạm vi tổ chức của caller.</summary>
    Task<IEnumerable<AssignableRoleResponse>> GetAssignableRolesAsync(CancellationToken cancellationToken = default);
    /// <summary>Ủy quyền role cho user khác, qua chuỗi kiểm tra: caller phải có menu PERMISSIONDELEGATION, không ủy quyền cho chính mình, user đích và role đều thuộc phạm vi; bỏ qua nếu user đã có role. Audit sự kiện Critical và xóa cache menu của user đích.</summary>
    Task AssignRoleAsync(AssignRoleRequest request, CancellationToken cancellationToken = default);
    /// <summary>Thu hồi ủy quyền role với chuỗi kiểm tra tương tự AssignRoleAsync; bỏ qua nếu user đích không giữ role đó. Audit và xóa cache menu của user đích.</summary>
    Task RevokeRoleAsync(RevokeRoleRequest request, CancellationToken cancellationToken = default);
    /// <summary>Quyền hiệu dụng của một user: các role đang giữ và các menu nhận qua role (chỉ menu hoạt động) — dùng để xem lại ủy quyền. Trả null nếu user không tồn tại.</summary>
    Task<UserEffectivePermissionsResponse?> GetUserEffectivePermissionsAsync(int userId, CancellationToken cancellationToken = default);
}
