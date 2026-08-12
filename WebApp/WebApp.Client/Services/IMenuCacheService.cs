using Hgs.Share.Responses.Menus;

namespace WebApp.Client.Services;

public interface IMenuCacheService
{
    Task<List<MenusGetByUserIdResponse>?> GetCachedMenusAsync();
    Task SetCachedMenusAsync(List<MenusGetByUserIdResponse> menus);
    Task ClearCachedMenusAsync();
}
