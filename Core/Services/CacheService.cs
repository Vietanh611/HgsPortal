using Core.Interfaces;
using Microsoft.Extensions.Caching.Memory;

namespace Core.Services;

public class CacheService : ICacheService
{
    private readonly IMemoryCache _memoryCache;
    private static readonly object _lock = new object();
    private static readonly List<string> _menuCacheKeys = new List<string>();

    public CacheService(IMemoryCache memoryCache)
    {
        _memoryCache = memoryCache;
    }

    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        return await Task.FromResult(_memoryCache.Get<T>(key));
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? absoluteExpiration = null, CancellationToken cancellationToken = default)
    {
        var options = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = absoluteExpiration ?? TimeSpan.FromMinutes(5)
        };

        _memoryCache.Set(key, value, options);

        // Track menu cache keys for invalidation
        if (key.StartsWith("menus:user:"))
        {
            lock (_lock)
            {
                if (!_menuCacheKeys.Contains(key))
                {
                    _menuCacheKeys.Add(key);
                }
            }
        }

        await Task.CompletedTask;
    }

    public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        _memoryCache.Remove(key);

        lock (_lock)
        {
            _menuCacheKeys.Remove(key);
        }

        await Task.CompletedTask;
    }

    public async Task RemoveByPrefixAsync(string prefix, CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            var keysToRemove = _menuCacheKeys.Where(k => k.StartsWith(prefix)).ToList();
            foreach (var key in keysToRemove)
            {
                _memoryCache.Remove(key);
                _menuCacheKeys.Remove(key);
            }
        }
        await Task.CompletedTask;
    }

    public async Task ClearUserMenuCacheAsync(int userId, CancellationToken cancellationToken = default)
    {
        await RemoveAsync($"menus:user:{userId}", cancellationToken);
        await RemoveAsync($"menus:user:codes:{userId}", cancellationToken);
    }

    public async Task ClearAllMenuCacheAsync(CancellationToken cancellationToken = default)
    {
        await RemoveByPrefixAsync("menus:user:", cancellationToken);
    }
}
