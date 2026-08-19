using Domain.Entities.Identity;
using Hgs.Share.Requests.UserRoles;

namespace Core.Interfaces.Identity;

public interface IUserRoleService
{
    Task<IEnumerable<UserRoles>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<UserRoles?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<IEnumerable<UserRoles>> GetByUserIdAsync(int userId, CancellationToken cancellationToken = default);
    Task<IEnumerable<UserRoles>> GetByRoleIdAsync(int roleId, CancellationToken cancellationToken = default);
    /// <summary>Gán role cho user: role phải assignable (đang hoạt động, không phải role hệ thống, thuộc phạm vi tổ chức của caller). Tự động copy các menu của role thành UserMenus của user và xóa toàn bộ cache menu.</summary>
    Task<UserRoles> CreateAsync(UserRolesCreateRequest request, int assignedBy, CancellationToken cancellationToken = default);
    Task<UserRoles?> UpdateAsync(int id, UserRolesUpdateRequest request, CancellationToken cancellationToken = default);
    /// <summary>Gỡ role khỏi user; từ chối gỡ role cuối cùng để user không bao giờ bị mất hết role. Xóa cache menu.</summary>
    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);
    /// <summary>Gán nhiều role cùng lúc; bỏ qua role đã được gán trước đó (idempotent) và tự động copy menu của từng role mới vào UserMenus của user. Xóa cache menu.</summary>
    Task AssignMultipleRolesAsync(int userId, List<int> roleIds, int assignedBy, DateTime? expiredAt = null, CancellationToken cancellationToken = default);
    /// <summary>Gỡ nhiều role; từ chối nếu thao tác khiến user không còn role nào. Xóa cache menu.</summary>
    Task RemoveMultipleRolesAsync(int userId, List<int> roleIds, CancellationToken cancellationToken = default);
}
