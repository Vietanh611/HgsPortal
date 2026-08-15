using Hgs.Share.Responses.Menus;

namespace WebApp.Client.Services;

public interface IMenuCacheService
{
    Task<List<MenusGetByUserIdResponse>?> GetCachedMenusAsync(int userId);
    Task SetCachedMenusAsync(int userId, List<MenusGetByUserIdResponse> menus);
    Task ClearCachedMenusAsync(int? userId = null);
}