using Blazored.LocalStorage;
using Hgs.Share.Responses.Menus;
using System.Text.Json;

namespace WebApp.Client.Services;

public class MenuCacheService : IMenuCacheService
{
    private readonly ILocalStorageService _localStorageService;
    private const string CachedMenusKeyPrefix = "cachedMenus_";
    private const string CachedMenusUserListKey = "cachedMenus_users";
    private static readonly TimeSpan MenuCacheTtl = TimeSpan.FromMinutes(30);

    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public MenuCacheService(ILocalStorageService localStorageService)
    {
        _localStorageService = localStorageService;
    }

    private static string GetKey(int userId) => $"{CachedMenusKeyPrefix}{userId}";

    public async Task<List<MenusGetByUserIdResponse>?> GetCachedMenusAsync(int userId)
    {
        try
        {
            var menusJson = await _localStorageService.GetItemAsStringAsync(GetKey(userId));
            if (string.IsNullOrEmpty(menusJson))
            {
                return null;
            }

            CachedMenusEnvelope? envelope = null;
            try
            {
                envelope = JsonSerializer.Deserialize<CachedMenusEnvelope>(menusJson, _jsonOptions);
            }
            catch
            {
                envelope = null;
            }

            if (envelope?.Menus is null || envelope.SavedAtUtc == default ||
                DateTime.UtcNow - envelope.SavedAtUtc > MenuCacheTtl)
            {
                await RemoveCachedMenusAsync(userId);
                return null;
            }

            return envelope.Menus;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error reading cached menus: {ex.Message}");
            return null;
        }
    }

    public async Task SetCachedMenusAsync(int userId, List<MenusGetByUserIdResponse> menus)
    {
        try
        {
            var envelope = new CachedMenusEnvelope
            {
                SavedAtUtc = DateTime.UtcNow,
                Menus = menus
            };

            await _localStorageService.SetItemAsStringAsync(GetKey(userId), JsonSerializer.Serialize(envelope, _jsonOptions));

            await AddToTrackingAsync(userId);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error saving cached menus: {ex.Message}");
        }
    }

    public async Task ClearCachedMenusAsync(int? userId = null)
    {
        try
        {
            if (userId.HasValue)
            {
                await RemoveCachedMenusAsync(userId.Value);
                return;
            }

            var tracked = await GetTrackedUsersAsync();
            foreach (var id in tracked)
            {
                await _localStorageService.RemoveItemAsync(GetKey(id));
            }

            await _localStorageService.RemoveItemAsync(CachedMenusUserListKey);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error clearing cached menus: {ex.Message}");
        }
    }

    private async Task RemoveCachedMenusAsync(int userId)
    {
        await _localStorageService.RemoveItemAsync(GetKey(userId));
        await RemoveFromTrackingAsync(userId);
    }

    private async Task<List<int>> GetTrackedUsersAsync()
    {
        try
        {
            var trackedJson = await _localStorageService.GetItemAsStringAsync(CachedMenusUserListKey);
            if (string.IsNullOrWhiteSpace(trackedJson))
            {
                return [];
            }

            return JsonSerializer.Deserialize<List<int>>(trackedJson) ?? [];
        }
        catch
        {
            return [];
        }
    }

    private async Task AddToTrackingAsync(int userId)
    {
        var tracked = await GetTrackedUsersAsync();
        if (!tracked.Contains(userId))
        {
            tracked.Add(userId);
            await _localStorageService.SetItemAsStringAsync(
                CachedMenusUserListKey,
                JsonSerializer.Serialize(tracked));
        }
    }

    private async Task RemoveFromTrackingAsync(int userId)
    {
        var tracked = await GetTrackedUsersAsync();
        if (!tracked.Contains(userId))
        {
            return;
        }

        tracked.Remove(userId);
        if (tracked.Count == 0)
        {
            await _localStorageService.RemoveItemAsync(CachedMenusUserListKey);
        }
        else
        {
            await _localStorageService.SetItemAsStringAsync(
                CachedMenusUserListKey,
                JsonSerializer.Serialize(tracked));
        }
    }

    private sealed class CachedMenusEnvelope
    {
        public DateTime SavedAtUtc { get; set; }
        public List<MenusGetByUserIdResponse>? Menus { get; set; }
    }
}
