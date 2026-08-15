using Domain.Entities.Identity;

namespace Core.Interfaces;

public interface IOrgScopeService
{
    /// <summary>Paths của các org unit mà caller quản lý (anchor = User.OrganizationUnitId của caller, gồm cấp con). null = SUPER_ADMIN (tất cả).</summary>
    Task<List<string>?> GetCallerScopePathsAsync(CancellationToken cancellationToken = default);

    Task<bool> IsOrgUnitInScopeAsync(int orgUnitId, CancellationToken cancellationToken = default);

    Task<bool> IsUserInScopeAsync(int targetUserId, CancellationToken cancellationToken = default);

    Task<bool> IsRoleAssignableAsync(int roleId, CancellationToken cancellationToken = default);

    Task<IEnumerable<Roles>> GetAssignableRolesAsync(CancellationToken cancellationToken = default);
}
