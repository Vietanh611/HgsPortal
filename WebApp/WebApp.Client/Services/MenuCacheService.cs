using Blazored.LocalStorage;
using Hgs.Share.Responses.Menus;
using System.Text.Json;

namespace WebApp.Client.Services;

public class MenuCacheService : IMenuCacheService
{
    private readonly ILocalStorageService _localStorageService;
    private const string CachedMenusKey = "cachedMenus";

    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public MenuCacheService(ILocalStorageService localStorageService)
    {
        _localStorageService = localStorageService;
    }

    public async Task<List<MenusGetByUserIdResponse>?> GetCachedMenusAsync()
    {
        try
        {
            var menusJson = await _localStorageService.GetItemAsStringAsync(CachedMenusKey);
            if (string.IsNullOrEmpty(menusJson))
            {
                return null;
            }

            return JsonSerializer.Deserialize<List<MenusGetByUserIdResponse>>(menusJson, _jsonOptions);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error reading cached menus: {ex.Message}");
            return null;
        }
    }

    public async Task SetCachedMenusAsync(List<MenusGetByUserIdResponse> menus)
    {
        try
        {
            var menusJson = JsonSerializer.Serialize(menus, _jsonOptions);
            await _localStorageService.SetItemAsStringAsync(CachedMenusKey, menusJson);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error saving cached menus: {ex.Message}");
        }
    }

    public async Task ClearCachedMenusAsync()
    {
        try
        {
            await _localStorageService.RemoveItemAsync(CachedMenusKey);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error clearing cached menus: {ex.Message}");
        }
    }
}
