using Hgs.Share.Requests.PermissionDelegation;
using Hgs.Share.Responses.PermissionDelegation;

namespace Core.Interfaces;

public interface IPermissionDelegationService
{
    Task<IEnumerable<ManageableUserResponse>> GetManageableUsersAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<AssignableRoleResponse>> GetAssignableRolesAsync(CancellationToken cancellationToken = default);
    Task AssignRoleAsync(AssignRoleRequest request, CancellationToken cancellationToken = default);
    Task RevokeRoleAsync(RevokeRoleRequest request, CancellationToken cancellationToken = default);
    Task<UserEffectivePermissionsResponse?> GetUserEffectivePermissionsAsync(int userId, CancellationToken cancellationToken = default);
}
