namespace Core.Services.Settings;

public class JwtSettings
{
    public string Secret { get; set; } = string.Empty;
    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public int ExpiryMinutes { get; set; } = 60;

    /// <summary>
    /// Cửa sổ (giây) cho phép dùng lại refresh token vừa bị xoay vòng mà KHÔNG coi là đánh cắp.
    /// Cookie refresh_token dùng chung cho mọi tab của cùng một trình duyệt nên hai tab có thể
    /// gửi refresh gần đồng thời: tab thua gửi token đã revoke trong cửa sổ này → server xoay tiếp
    /// từ token active thay vì thu hồi phiên (cho phép mở nhiều tab cùng lúc). Ngoài cửa sổ,
    /// dùng lại token đã revoke vẫn bị báo động tái sử dụng trái phép. Giá trị 0 tắt hành vi này.
    /// </summary>
    public int RefreshReuseIntervalSeconds { get; set; } = 60;
}
