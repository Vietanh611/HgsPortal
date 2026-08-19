namespace WebApp.Client.Services.Auth;

/// <summary>
/// Kết quả refresh access token, giúp caller phân biệt trường hợp nào PHẢI đăng xuất và
/// trường hợp nào chỉ là lỗi tạm thời nên giữ phiên và thử lại sau:
/// - <see cref="Success"/>: token mới đã lưu vào storage.
/// - <see cref="SessionExpired"/>: refresh cookie không còn hợp lệ (revoke/hết hạn) hoặc
///   response không thể parse — phiên thực sự đã chết, cần đăng xuất.
/// - <see cref="NetworkError"/>: lỗi mạng/server tạm thời (5xx, 429) — KHÔNG đăng xuất,
///   chờ chu kỳ sau thử lại.
/// </summary>
public enum TokenRefreshResult
{
    Success,
    SessionExpired,
    NetworkError
}
