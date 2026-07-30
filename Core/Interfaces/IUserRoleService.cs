using Domain.Entities.Identity;
using Hgs.Share.Requests.UserRoles;

namespace Core.Interfaces;

public interface IUserRoleService
{
    Task<IEnumerable<UserRoles>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<UserRoles?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<IEnumerable<UserRoles>> GetByUserIdAsync(int userId, CancellationToken cancellationToken = default);
    Task<IEnumerable<UserRoles>> GetByRoleIdAsync(int roleId, CancellationToken cancellationToken = default);
    Task<UserRoles> CreateAsync(UserRolesCreateRequest request, int assignedBy, CancellationToken cancellationToken = default);
    Task<UserRoles?> UpdateAsync(int id, UserRolesUpdateRequest request, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);
    Task AssignMultipleRolesAsync(int userId, List<int> roleIds, int assignedBy, DateTime? expiredAt = null, CancellationToken cancellationToken = default);
    Task RemoveMultipleRolesAsync(int userId, List<int> roleIds, CancellationToken cancellationToken = default);
}
