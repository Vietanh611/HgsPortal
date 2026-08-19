using API.Extensions;
using Core.Interfaces.Auth;
using Domain.Entities.Identity;
using Hgs.Share.Requests;
using Hgs.Share.Requests.Users;
using Hgs.Share.Responses;
using Hgs.Share.Responses.ApiResponses;
using Hgs.Share.Responses.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers.Auth;

[ApiController]
[Route("[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IAuthService authService, ILogger<AuthController> logger)
    {
        _authService = authService;
        _logger = logger;
    }

    /// <summary>
    /// Đăng nhập và trả access token; refresh token chỉ được đặt trong cookie HttpOnly
    /// `refresh_token` (7 ngày), không bao giờ nằm trong body response.
    /// </summary>
    [HttpPost("login")]
    [AllowAnonymous]
    [IgnoreAntiforgeryToken]
    public async Task<ActionResult<ApiResponse<AuthenticateResponse>>> Login(
        [FromBody] AuthenticateRequest request,
        CancellationToken cancellationToken)
    {
        var response = await _authService.LoginAsync(
            request,
            HttpContext.Request.Headers.UserAgent.ToString(),
            HttpContext.Connection.RemoteIpAddress?.ToString(),
            cancellationToken);

        // Set RefreshToken in HttpOnly cookie
        _authService.SetRefreshTokenCookie(HttpContext, response.RefreshToken);

        // Remove RefreshToken from response (it's now in cookie)
        response.RefreshToken = null;

        _logger.LogInformation("User '{Username}' authenticated successfully.", request.Username);
        return Ok(ApiResponse<AuthenticateResponse>.SuccessResponse(response, "Login successful", 200));
    }

    /// <summary>
    /// Làm mới access token bằng refresh token đọc từ cookie; token cũ bị thu hồi (rotation)
    /// và token mới được đặt lại vào cookie.
    /// </summary>
    [HttpPost("refresh-token")]
    [AllowAnonymous]
    [IgnoreAntiforgeryToken]
    public async Task<ActionResult<ApiResponse<AuthenticateResponse>>> RefreshToken(
        CancellationToken cancellationToken)
    {
        // Read RefreshToken from cookie
        var refreshToken = HttpContext.Request.Cookies["refresh_token"];
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            return BadRequest(ApiResponse<AuthenticateResponse>.FailResponse("Refresh token cookie not found", 400));
        }

        var request = new RefreshTokenRequest { RefreshToken = refreshToken };
        var response = await _authService.RefreshTokenAsync(
            request,
            HttpContext.Request.Headers.UserAgent.ToString(),
            HttpContext.Connection.RemoteIpAddress?.ToString(),
            cancellationToken);

        // Set new RefreshToken in HttpOnly cookie
        _authService.SetRefreshTokenCookie(HttpContext, response.RefreshToken);

        // Remove RefreshToken from response (it's now in cookie)
        response.RefreshToken = null;

        return Ok(ApiResponse<AuthenticateResponse>.SuccessResponse(response, "Token refreshed successfully", 200));
    }

    /// <summary>
    /// Gửi link đặt lại mật khẩu qua email; luôn trả về thành công kể cả khi email không tồn tại
    /// để tránh lộ thông tin tài khoản (chống enumeration).
    /// </summary>
    [HttpPost("forgot-password")]
    [AllowAnonymous]
    [IgnoreAntiforgeryToken]
    public async Task<ActionResult<ApiResponse>> ForgotPassword(
        [FromBody] ForgotPasswordRequest request,
        CancellationToken cancellationToken)
    {
        await _authService.ForgotPasswordAsync(request, cancellationToken);
        return Ok(ApiResponse.SuccessResponse("A reset link was sent.", 200));
    }

    /// <summary>
    /// Đặt lại mật khẩu bằng link dùng một lần (hết hạn 30 phút); đồng thời thu hồi mọi
    /// refresh token đang hoạt động của user — đăng xuất toàn bộ phiên.
    /// </summary>
    [HttpPost("reset-password")]
    [AllowAnonymous]
    [IgnoreAntiforgeryToken]
    public async Task<ActionResult<ApiResponse>> ResetPassword(
        [FromBody] ResetPasswordRequest request,
        CancellationToken cancellationToken)
    {
        await _authService.ResetPasswordAsync(request, cancellationToken);
        return Ok(ApiResponse.SuccessResponse("Password reset successfully.", 200));
    }

    /// <summary>
    /// Thu hồi refresh token phía server và xóa cookie `refresh_token`.
    /// </summary>
    [HttpPost("logout")]
    [AllowAnonymous]
    [IgnoreAntiforgeryToken]
    public async Task<ActionResult<ApiResponse>> Logout(
        CancellationToken cancellationToken)
    {
        // Read RefreshToken from cookie
        var refreshToken = HttpContext.Request.Cookies["refresh_token"];
        if (!string.IsNullOrWhiteSpace(refreshToken))
        {
            var request = new LogoutRequest { RefreshToken = refreshToken };
            await _authService.LogoutAsync(request, cancellationToken);
        }

        // Clear RefreshToken cookie
        _authService.ClearRefreshTokenCookie(HttpContext);

        return Ok(ApiResponse.SuccessResponse("Logout successful", 200));
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<UsersGetByIdResponse>>> GetCurrentUser(CancellationToken cancellationToken)
    {
        if (!User.TryGetCurrentUserId(out var userId))
        {
            return Unauthorized(ApiResponse<UsersGetByIdResponse>.FailResponse("Invalid user token", 401));
        }

        var user = await _authService.GetCurrentUserAsync(userId, cancellationToken);
        if (user is null)
        {
            return NotFound(ApiResponse<UsersGetByIdResponse>.FailResponse("User not found", 404));
        }

        return Ok(ApiResponse<UsersGetByIdResponse>.SuccessResponse(MapToUserResponse(user), "User retrieved successfully", 200));
    }

    private UsersGetByIdResponse MapToUserResponse(Users user) => new()
    {
        Id = user.Id,
        Username = user.Username,
        Email = user.Email,
        FullName = user.FullName,
        PhoneNumber = user.PhoneNumber,
        AvatarUrl = ResolveUrlPath(user.AvatarUrl),
        BravoId = user.BravoId,
        OrganizationUnitId = user.OrganizationUnitId,
        OrganizationUnitName = user.OrganizationUnit?.Name,
        IsActive = user.IsActive,
        IsLocked = user.IsLocked,
        LockoutEnd = user.LockoutEnd,
        FailedLoginCount = user.FailedLoginCount,
        LastLoginAt = user.LastLoginAt,
        CreatedAt = user.CreatedAt,
        CreatedBy = user.CreatedBy,
        UpdatedAt = user.UpdatedAt,
        UpdatedBy = user.UpdatedBy,
        IsDeleted = user.IsDeleted
    };

    private string? ResolveUrlPath(string? urlPath)
    {
        return UrlPathResolver.Resolve(Request, urlPath);
    }
}
