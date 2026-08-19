using Domain.Entities.Identity;
using Hgs.Share.Requests;
using Hgs.Share.Requests.Users;
using Hgs.Share.Responses;
using Microsoft.AspNetCore.Http;

namespace Core.Interfaces.Auth;

public interface IAuthService
{
    /// <summary>
    /// Đăng nhập bằng username/password. Tài khoản đăng nhập sai quá LockoutSettings.MaxFailedAttempts
    /// lần liên tiếp bị khóa tạm thời (LockoutMinutes). Lỗi sai mật khẩu và username không tồn tại dùng
    /// chung một thông báo để không phân biệt được tài khoản hợp lệ (chống user enumeration).
    /// </summary>
    Task<AuthenticateResponse> LoginAsync(
        AuthenticateRequest request,
        string? userAgent,
        string? ipAddress,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Xoay vòng (rotate) refresh token: token cũ bị thu hồi ngay và cấp token mới cùng token family.
    /// Token đã thu hồi/hết hạn bị dùng lại sẽ ghi sự kiện bảo mật REFRESH_TOKEN_REUSE_DETECTED
    /// — dấu hiệu token bị đánh cắp.
    /// </summary>
    Task<AuthenticateResponse> RefreshTokenAsync(
        RefreshTokenRequest request,
        string? userAgent,
        string? ipAddress,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Thu hồi (revoke) refresh token ở phía server để phiên đã đăng xuất không thể được tái sử dụng.
    /// </summary>
    Task LogoutAsync(
        LogoutRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gửi email đặt lại mật khẩu. Với email không tồn tại hoặc không hoạt động, method trả về thành công
    /// im lặng (không log, không ném lỗi) để không lộ tài khoản hợp lệ qua hành vi khác nhau (chống user enumeration).
    /// </summary>
    Task ForgotPasswordAsync(
        ForgotPasswordRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Đặt lại mật khẩu bằng token dùng một lần (hết hạn sau 30 phút). Sau khi đổi mật khẩu, toàn bộ
    /// refresh token đang hoạt động của tài khoản bị thu hồi để buộc đăng nhập lại từ đầu.
    /// </summary>
    Task ResetPasswordAsync(
        ResetPasswordRequest request,
        CancellationToken cancellationToken = default);

    Task<Users?> GetCurrentUserAsync(
        int userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Ghi refresh token vào cookie HttpOnly + SameSite để JavaScript phía client không đọc được
    /// (giảm rủi ro đánh cắp token qua XSS).
    /// </summary>
    void SetRefreshTokenCookie(HttpContext context, string token);

    /// <summary>
    /// Xóa cookie refresh token với cùng Path/Secure/SameSite như lúc set — cookie chỉ bị xóa khi thuộc tính khớp.
    /// </summary>
    void ClearRefreshTokenCookie(HttpContext context);
}
