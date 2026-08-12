using Hgs.Share.Responses.Menus;
using WebApp.Client.Services;

namespace WebApp.Services;

public class ServerMenuCacheService : IMenuCacheService
{
    private List<MenusGetByUserIdResponse>? _cachedMenus;

    public Task<List<MenusGetByUserIdResponse>?> GetCachedMenusAsync()
    {
        return Task.FromResult(_cachedMenus);
    }

    public Task SetCachedMenusAsync(List<MenusGetByUserIdResponse> menus)
    {
        _cachedMenus = menus;
        return Task.CompletedTask;
    }

    public Task ClearCachedMenusAsync()
    {
        _cachedMenus = null;
        return Task.CompletedTask;
    }
}
