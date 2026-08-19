namespace Core.Interfaces.Operations;

public interface ICacheService
{
    Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default);

    /// <summary>Ghi cache; khi không truyền <c>absoluteExpiration</c>, entry hết hạn sau 5 phút (DefaultTtl).</summary>
    Task SetAsync<T>(string key, T value, TimeSpan? absoluteExpiration = null, CancellationToken cancellationToken = default);

    Task RemoveAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>Xóa các key theo prefix. IMemoryCache không hỗ trợ liệt kê key nên chỉ xóa được các key đã được đăng ký theo dõi khi ghi cache (key dạng "menus:user:" / "users:superadmin:").</summary>
    Task RemoveByPrefixAsync(string prefix, CancellationToken cancellationToken = default);

    /// <summary>Hủy toàn bộ cache menu/superadmin của một user (menus:user:{id}, menus:user:codes:{id}, users:superadmin:{id}) để thay đổi menu/quyền của user có hiệu lực ngay.</summary>
    Task ClearUserMenuCacheAsync(int userId, CancellationToken cancellationToken = default);

    /// <summary>Hủy toàn bộ cache menu của mọi user — dùng khi cấu trúc menu hoặc định nghĩa quyền thay đổi toàn cục.</summary>
    Task ClearAllMenuCacheAsync(CancellationToken cancellationToken = default);
}
