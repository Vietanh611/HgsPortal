using System.Collections.Concurrent;
using Hgs.Share.Responses.Menus;
using Microsoft.Extensions.Caching.Memory;
using WebApp.Client.Services;

namespace WebApp.Services;

public class ServerMenuCacheService : IMenuCacheService
{
    private readonly IMemoryCache _cache;
    private static readonly TimeSpan Ttl = TimeSpan.FromMinutes(30);
    private readonly ConcurrentDictionary<int, byte> _trackedUsers = new();

    public ServerMenuCacheService(IMemoryCache cache)
    {
        _cache = cache;
    }

    private static string GetKey(int userId) => $"menus:user:{userId}";

    public Task<List<MenusGetByUserIdResponse>?> GetCachedMenusAsync(int userId)
    {
        return Task.FromResult(_cache.Get<List<MenusGetByUserIdResponse>>(GetKey(userId)));
    }

    public Task SetCachedMenusAsync(int userId, List<MenusGetByUserIdResponse> menus)
    {
        _cache.Set(GetKey(userId), menus, new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = Ttl,
            Size = 1
        });

        _trackedUsers.TryAdd(userId, 0);
        return Task.CompletedTask;
    }

    public Task ClearCachedMenusAsync(int? userId = null)
    {
        if (userId.HasValue)
        {
            _cache.Remove(GetKey(userId.Value));
            _trackedUsers.TryRemove(userId.Value, out _);
        }
        else
        {
            foreach (var id in _trackedUsers.Keys)
            {
                _cache.Remove(GetKey(id));
            }

            _trackedUsers.Clear();
        }

        return Task.CompletedTask;
    }
}