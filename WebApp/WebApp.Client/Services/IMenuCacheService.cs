using Hgs.Share.Responses.Menus;

namespace WebApp.Client.Services;

/// <summary>
/// Hợp đồng cache menu theo user; có triển khai localStorage phía client và triển khai
/// no-op phía server (prerendering).
/// </summary>
public interface IMenuCacheService
{
    Task<List<MenusGetByUserIdResponse>?> GetCachedMenusAsync(int userId);
    Task SetCachedMenusAsync(int userId, List<MenusGetByUserIdResponse> menus);
    Task ClearCachedMenusAsync(int? userId = null);
}