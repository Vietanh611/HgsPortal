using Domain.Entities.System;
using Hgs.Share.Dtos;
using Hgs.Share.Requests.UserMenus;
using Hgs.Share.Responses.Menus;
using Hgs.Share.Responses.UserMenus;

namespace Core.Interfaces;

public interface IUserMenuService
{
    Task<IEnumerable<UserMenuDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<MenusGetByUserIdResponse>> GetByUserIdAsync(int userId, CancellationToken cancellationToken = default);
    Task<IEnumerable<int>> GetMenuIdsByUserIdAsync(int userId, CancellationToken cancellationToken = default);
    Task<UserMenuAssignmentDetailsResponse> GetAssignmentDetailsByUserIdAsync(int userId, CancellationToken cancellationToken = default);
    Task<UserMenus> CreateAsync(UserMenusCreateRequest request, int assignedBy, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);
    Task<bool> AssignMultipleMenusAsync(int userId, List<int> menuIds, int assignedBy, CancellationToken cancellationToken = default);
    Task<bool> RemoveMultipleMenusAsync(int userId, List<int> menuIds, CancellationToken cancellationToken = default);
}
