using Domain.Entities.System;
using Hgs.Share.Requests.Menus;
using Hgs.Share.Responses.Menus;

namespace Core.Interfaces;

public interface IMenuService
{
    Task<IEnumerable<Menus>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<Menus?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<Menus> CreateAsync(MenusCreateRequest request, CancellationToken cancellationToken = default);
    Task<Menus?> UpdateAsync(int id, MenusUpdateRequest request, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);
    Task<List<MenusGetByUserIdResponse>> GetMenusByUserIdAsync(int userId, CancellationToken cancellationToken = default);
}
