using Domain.Entities.Identity;

namespace Core.Interfaces;

public interface IOrgScopeService
{
    /// <summary>Paths của các org unit mà caller quản lý (anchor = User.OrganizationUnitId của caller, gồm cấp con). null = SUPER_ADMIN (tất cả).</summary>
    Task<List<string>?> GetCallerScopePathsAsync(CancellationToken cancellationToken = default);

    /// <summary>Kiểm tra org unit thuộc phạm vi quản lý của caller: org bằng hoặc là cấp con theo Path của caller. SUPER_ADMIN luôn hợp lệ.</summary>
    Task<bool> IsOrgUnitInScopeAsync(int orgUnitId, CancellationToken cancellationToken = default);

    /// <summary>Kiểm tra user mục tiêu thuộc phạm vi quản lý của caller (org của user bằng hoặc nằm dưới org caller theo Path). Chính caller luôn in-scope; user đã xóa (soft-delete) ngoài phạm vi.</summary>
    Task<bool> IsUserInScopeAsync(int targetUserId, CancellationToken cancellationToken = default);

    /// <summary>Kiểm tra role có thể được gán bởi caller: phải active, không phải role hệ thống (IsSystemRole), có gắn org và org đó thuộc phạm vi quản lý của caller. SUPER_ADMIN gán được mọi role không phải hệ thống.</summary>
    Task<bool> IsRoleAssignableAsync(int roleId, CancellationToken cancellationToken = default);

    /// <summary>Trả về các role caller có thể gán: active, không phải role hệ thống, thuộc org trong phạm vi quản lý. SUPER_ADMIN nhận toàn bộ role active không phải hệ thống.</summary>
    Task<IEnumerable<Roles>> GetAssignableRolesAsync(CancellationToken cancellationToken = default);
}
