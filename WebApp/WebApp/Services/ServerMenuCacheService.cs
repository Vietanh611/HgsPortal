using System.Collections.Concurrent;
using Hgs.Share.Responses.Menus;
using Microsoft.Extensions.Caching.Memory;
using WebApp.Client.Services;

namespace WebApp.Services;

/// <summary>
/// Triển khai phía server của IMenuCacheService dùng khi prerendering (localStorage của client
/// WASM chưa sẵn sàng). Đây là cache thật trong bộ nhớ server (IMemoryCache), TTL 30 phút đồng
/// bộ với bản client (MenuCacheService) và theo dõi các user để xóa hàng loạt khi phân quyền đổi.
/// </summary>
/// <remarks>
/// Cache nằm trong bộ nhớ của một instance server — không chia sẻ giữa các instance; sau TTL
/// menu được nạp lại từ API nên thay đổi phân quyền sẽ tự phản ánh.
/// </remarks>
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

    /// <summary>
    /// Ghi menu của user vào cache với TTL 30 phút và ghi nhận user vào danh sách theo dõi để
    /// ClearCachedMenusAsync(null) xóa được hàng loạt khi phân quyền thay đổi.
    /// </summary>
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

    /// <summary>
    /// Xóa cache của một user; khi không truyền userId sẽ xóa toàn bộ cache đang theo dõi —
    /// được gọi khi phân quyền thay đổi để menu nạp lại theo quyền mới.
    /// </summary>
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