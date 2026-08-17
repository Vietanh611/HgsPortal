using Core.Helpers;
using Core.Interfaces;
using Core.Services.Notifications;
using Core.Services.Settings;
using Data.DbContexts;
using Domain.Entities.Identity;
using Hgs.Share.Exceptions;
using Hgs.Share.Requests;
using Hgs.Share.Requests.Users;
using Hgs.Share.Responses;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography;

namespace Core.Services.Auth;

public class AuthService : IAuthService
{
    private readonly HgsDbContext _dbContext;
    private readonly ITokenService _tokenService;
    private readonly IMailService _mailService;
    private readonly IAuditLogService _auditLog;
    private readonly JwtSettings _jwtSettings;
    private readonly CookieSettings _cookieSettings;
    private readonly LockoutSettings _lockoutSettings;
    private readonly MailSettings _mailSettings;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        HgsDbContext dbContext,
        ITokenService tokenService,
        IMailService mailService,
        IAuditLogService auditLog,
        IOptions<JwtSettings> jwtSettings,
        IOptions<CookieSettings> cookieSettings,
        IOptions<LockoutSettings> lockoutSettings,
        IOptions<MailSettings> mailSettings,
        ILogger<AuthService> logger)
    {
        _dbContext = dbContext;
        _tokenService = tokenService;
        _mailService = mailService;
        _auditLog = auditLog;
        _jwtSettings = jwtSettings.Value;
        _cookieSettings = cookieSettings.Value;
        _lockoutSettings = lockoutSettings.Value;
        _mailSettings = mailSettings.Value;
        _logger = logger;
    }

    public async Task<AuthenticateResponse> LoginAsync(
        AuthenticateRequest request,
        string? userAgent,
        string? ipAddress,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
        {
            _logger.LogWarning("Login attempt with missing username or password.");
            throw new BadRequestException("Username and password are required.");
        }

        var user = await _dbContext.Users
            .Where(u => u.Username == request.Username && !u.IsDeleted)
            .FirstOrDefaultAsync(cancellationToken);

        if (user is null || !PasswordHelper.VerifyPassword(request.Password, user.PasswordHash))
        {
            _logger.LogWarning("Invalid login attempt for user '{Username}'.", request.Username);
            if (user is null)
            {
                // Không có UserId để gán — denormalize Username vẫn ghi nhận được (chống brute-force theo tài khoản)
                await _auditLog.LogSecurityEventAsync(
                    action: "LOGIN_FAIL_INVALID_CREDENTIALS",
                    eventCategory: "Auth", success: false, severity: "Warning",
                    username: request.Username,
                    detail: "Username không tồn tại");
            }
            else
            {
                await _auditLog.LogSecurityEventAsync(
                    action: "LOGIN_FAIL_INVALID_CREDENTIALS",
                    eventCategory: "Auth", success: false, severity: "Warning",
                    userId: user.Id, username: user.Username,
                    detail: "Sai mật khẩu");

                user.FailedLoginCount++;
                if (user.FailedLoginCount >= _lockoutSettings.MaxFailedAttempts)
                {
                    user.LockoutEnd = DateTime.UtcNow.AddMinutes(_lockoutSettings.LockoutMinutes);
                    user.FailedLoginCount = 0;
                    _logger.LogWarning("User '{Username}' locked out until {LockoutEnd}.", request.Username, user.LockoutEnd);

                    await _auditLog.LogSecurityEventAsync(
                        action: "ACCOUNT_LOCKED",
                        eventCategory: "Security", success: true, severity: "Critical",
                        targetUserId: user.Id, username: user.Username,
                        detail: "Khóa tự động sau khi vượt ngưỡng đăng nhập sai");
                }
                await _dbContext.SaveChangesAsync(cancellationToken);
            }
            throw new UnauthorizedException("Invalid username or password.");
        }

        if (user.LockoutEnd.HasValue && user.LockoutEnd > DateTime.UtcNow)
        {
            var minutesLeft = Math.Ceiling((user.LockoutEnd.Value - DateTime.UtcNow).TotalMinutes);
            _logger.LogWarning("Locked user '{Username}' tried to login.", request.Username);

            await _auditLog.LogSecurityEventAsync(
                action: "LOGIN_FAIL_LOCKED",
                eventCategory: "Auth", success: false, severity: "Warning",
                userId: user.Id, username: user.Username,
                detail: $"Tài khoản đang bị khóa, thử lại sau {minutesLeft} phút");

            throw new UnauthorizedException($"Account is temporarily locked. Try again in {minutesLeft} minute(s).");
        }

        if (!user.IsActive)
        {
            _logger.LogWarning("Inactive user '{Username}' tried to login.", request.Username);

            await _auditLog.LogSecurityEventAsync(
                action: "LOGIN_FAIL_INACTIVE_USER",
                eventCategory: "Auth", success: false, severity: "Warning",
                userId: user.Id, username: user.Username,
                detail: "Tài khoản đã bị vô hiệu hóa");

            throw new UnauthorizedException("User is not active.");
        }

        user.LastLoginAt = DateTime.UtcNow;
        if (user.FailedLoginCount != 0)
        {
            user.FailedLoginCount = 0;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditLog.LogSecurityEventAsync(
            action: "LOGIN_SUCCESS",
            eventCategory: "Auth", success: true, severity: "Info",
            userId: user.Id, username: user.Username);

        return await IssueTokensAsync(user, userAgent, ipAddress, cancellationToken);
    }

    public async Task<AuthenticateResponse> RefreshTokenAsync(
        RefreshTokenRequest request,
        string? userAgent,
        string? ipAddress,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
        {
            throw new BadRequestException("Refresh token is required.");
        }

        var tokenHash = _tokenService.HashRefreshToken(request.RefreshToken);
        var tokenEntity = await _dbContext.RefreshTokens
            .Include(rt => rt.User)
            .Where(rt => rt.TokenHash == tokenHash)
            .FirstOrDefaultAsync(cancellationToken);

        if (tokenEntity is null)
        {
            // Token không còn khớp bản ghi hợp lệ — nhưng có thể là token CŨ đã bị rotate ra.
            // Nếu hash khớp PreviousTokenHash của một token đã revoke → tái sử dụng token cũ (dấu hiệu bị đánh cắp).
            var reusedOldToken = await _dbContext.RefreshTokens
                .AnyAsync(rt => rt.PreviousTokenHash == tokenHash && rt.IsRevoked, cancellationToken);

            if (reusedOldToken)
            {
                _logger.LogWarning("Refresh token reuse attempt detected (replayed old token).");

                await _auditLog.LogSecurityEventAsync(
                    action: "REFRESH_TOKEN_REUSE_DETECTED",
                    eventCategory: "Security", success: false, severity: "Critical",
                    detail: "Phát hiện sử dụng lại refresh token cũ đã bị thu hồi (PreviousTokenHash)");
            }

            throw new UnauthorizedException("Invalid or expired refresh token.");
        }

        if (tokenEntity.IsRevoked || tokenEntity.ExpiresAt < DateTime.UtcNow)
        {
            if (tokenEntity.IsRevoked)
            {
                // Token này ĐÃ bị revoke (sau khi rotate / logout) nhưng vẫn được dùng lại → reuse detection
                _logger.LogWarning("Refresh token reuse attempt detected (revoked token for user {UserId}).", tokenEntity.UserId);

                await _auditLog.LogSecurityEventAsync(
                    action: "REFRESH_TOKEN_REUSE_DETECTED",
                    eventCategory: "Security", success: false, severity: "Critical",
                    userId: tokenEntity.UserId,
                    detail: "Phát hiện sử dụng lại refresh token đã bị thu hồi");
            }

            throw new UnauthorizedException("Invalid or expired refresh token.");
        }

        var user = tokenEntity.User;
        if (user is null || user.IsDeleted || !user.IsActive)
        {
            throw new UnauthorizedException("Invalid user for refresh token.");
        }

        var newAccessToken = _tokenService.GenerateAccessToken(user.Id, user.Username);
        var newJwtId = new JwtSecurityTokenHandler().ReadJwtToken(newAccessToken).Id;
        var newRefreshToken = _tokenService.GenerateRefreshToken();

        tokenEntity.IsRevoked = true;
        tokenEntity.RevokedAt = DateTime.UtcNow;
        tokenEntity.ReplacedByToken = newRefreshToken;

        await SaveRefreshTokenAsync(user.Id, newRefreshToken, newJwtId, userAgent, ipAddress, tokenEntity.TokenFamily, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return BuildAuthenticateResponse(newAccessToken, newRefreshToken);
    }

    public async Task LogoutAsync(LogoutRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
        {
            throw new BadRequestException("Refresh token is required.");
        }

        var tokenHash = _tokenService.HashRefreshToken(request.RefreshToken);
        var tokenEntity = await _dbContext.RefreshTokens
            .Where(rt => rt.TokenHash == tokenHash)
            .FirstOrDefaultAsync(cancellationToken);

        if (tokenEntity is null)
        {
            throw new NotFoundException("Refresh token not found.");
        }

        if (!tokenEntity.IsRevoked)
        {
            tokenEntity.IsRevoked = true;
            tokenEntity.RevokedAt = DateTime.UtcNow;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditLog.LogSecurityEventAsync(
            action: "LOGOUT",
            eventCategory: "Auth", success: true, severity: "Info",
            userId: tokenEntity.UserId);
    }

    private async Task<AuthenticateResponse> IssueTokensAsync(Users user, string? userAgent, string? ipAddress, CancellationToken cancellationToken)
    {
        var accessToken = _tokenService.GenerateAccessToken(user.Id, user.Username);
        var accessJwtId = new JwtSecurityTokenHandler().ReadJwtToken(accessToken).Id;
        var refreshToken = _tokenService.GenerateRefreshToken();

        await SaveRefreshTokenAsync(user.Id, refreshToken, accessJwtId, userAgent, ipAddress, null, cancellationToken);

        return BuildAuthenticateResponse(accessToken, refreshToken);
    }

    private AuthenticateResponse BuildAuthenticateResponse(string accessToken, string refreshToken)
    {
        var expires = new JwtSecurityTokenHandler().ReadJwtToken(accessToken).ValidTo;
        return new AuthenticateResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpiresAt = expires
        };
    }

    private async Task SaveRefreshTokenAsync(int userId, string refreshToken, string jwtId, string? userAgent, string? ipAddress, Guid? tokenFamily, CancellationToken cancellationToken)
    {
        var tokenHash = _tokenService.HashRefreshToken(refreshToken);
        var currentTokenFamily = tokenFamily ?? _tokenService.GenerateTokenFamily();
        var tokenEntity = new RefreshTokens
        {
            UserId = userId,
            TokenHash = tokenHash,
            JwtId = jwtId,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            UserAgent = userAgent,
            IsRevoked = false,
            CreatedByIp = ipAddress,
            TokenFamily = currentTokenFamily,
            PreviousTokenHash = tokenFamily != null ? tokenHash : null
        };
        var isMobile = IsMobile(userAgent);
        var tokens = await _dbContext.RefreshTokens.Where(x => x.UserId == userId && !x.IsRevoked).ToListAsync();

        foreach (var token in tokens)
        {
            // 1 session mobile + 1 session desktop.
            if (IsMobile(token.UserAgent) == isMobile)
            {
                token.IsRevoked = true;
                token.RevokedAt = DateTime.UtcNow;
            }
        }
        await _dbContext.RefreshTokens.AddAsync(tokenEntity, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
    public static bool IsMobile(string? userAgent)
    {
        if (string.IsNullOrWhiteSpace(userAgent))
            return false;

        userAgent = userAgent.ToLowerInvariant();

        return userAgent.Contains("android") ||
               userAgent.Contains("iphone") ||
               userAgent.Contains("ipad") ||
               userAgent.Contains("mobile");
    }

    public async Task ForgotPasswordAsync(ForgotPasswordRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Email))
        {
            throw new BadRequestException("Email is required.");
        }

        var user = await _dbContext.Users
            .Where(u => u.Email == request.Email && !u.IsDeleted && u.IsActive)
            .FirstOrDefaultAsync(cancellationToken);

        if (user is null)
        {
            _logger.LogWarning("Forgot-password requested for unknown/inactive email '{Email}'.", request.Email);
            return;
        }

        var token = GenerateResetToken();
        user.PasswordResetTokenHash = _tokenService.HashRefreshToken(token);
        user.PasswordResetTokenExpiresAt = DateTime.UtcNow.AddMinutes(30);
        await _dbContext.SaveChangesAsync(cancellationToken);

        // Không log khi email không tồn tại — tránh lộ enumeration qua chính audit log
        await _auditLog.LogSecurityEventAsync(
            action: "PASSWORD_RESET_REQUESTED",
            eventCategory: "Auth", success: true, severity: "Info",
            targetUserId: user.Id, username: user.Username);

        var resetUrl = $"{_mailSettings.ResetPasswordBaseUrl}/{Uri.EscapeDataString(token)}";
        var body = $"""
            <!DOCTYPE html>
            <html lang="vi">
              <head>
                <meta charset="UTF-8">
                <title>HGS Portal - Đặt lại mật khẩu</title>
              </head>
              <body style="margin:0;padding:0;background-color:#f4f6f9;font-family:Segoe UI,Arial,sans-serif;">
                <div style="max-width:700px;margin:30px auto;background:#ffffff;border:1px solid #dcdcdc;border-radius:8px;overflow:hidden;">
                  <!-- Header -->
                  <table width="100%" cellpadding="0" cellspacing="0" style="background:#17479E;">
                    <tr>
                      <td style="padding:15px 20px;">
                        <img src="https://portal.hgs.vn/ImagesUploads/logoHgs.png" alt="HGS" style="height:40px;vertical-align:middle;">
                        <span style="font-size:18px;font-weight:bold;color:#F58220;padding-left:15px;">HANOI GROUND SERVICES</span>
                      </td>
                    </tr>
                  </table>
                  <!-- Content -->
                  <div style="padding:35px 40px;color:#333;font-size:15px;line-height:1.8;">
                    <p>Kính gửi anh/chị , <strong>{user.FullName}</strong>,</p>
                    <p>Hệ thống HGS Portal đã nhận được yêu cầu đặt lại mật khẩu cho tài khoản của anh/chị với địa chỉ email: <strong>{user.Email}</strong>).</p>
                    <p>Vui lòng nhấn vào nút “Đặt lại mật khẩu” bên dưới để thiết lập mật khẩu mới. Liên kết có hiệu lực trong
                       <strong>30 phút</strong> và chỉ sử dụng được một lần.</p>
                    <p style="text-align:center;margin:30px 0;">
                      <a href="{resetUrl}" style="display:inline-block;background:#17479E;color:#ffffff;padding:12px 28px;border-radius:6px;text-decoration:none;font-weight:bold;">Đặt lại mật khẩu</a>
                    </p>
                    <p style="font-size:13px;color:#6C757D;">Nếu bạn không yêu cầu đặt lại mật khẩu, vui lòng bỏ qua email này.
                       Mật khẩu của bạn sẽ không thay đổi.</p>
                    <p>Trân trọng!</p>
                  </div>
                  <!-- Footer -->
                  <div style="background:#f7f7f7;border-top:1px solid #e5e5e5;padding:20px 35px;color:#555;font-size:13px;line-height:1.7;">
                    <strong style="color:#17479E;">HGS PORTAL</strong>
                    <br>
                    Hanoi Ground Services JSC (HGS)
                    <hr style="border:none;border-top:1px solid #dddddd;margin:18px 0;">
                    <div style="text-align:center;color:#888;">
                      &copy; {DateTime.UtcNow.Year} Hanoi Ground Services JSC. <br>
                      Email này được gửi tự động, vui lòng không phản hồi.
                    </div>
                  </div>
                </div>
              </body>
            </html>
            """;

        await _mailService.SendAsync(new MailMessage
        {
            To = user.Email,
            Subject = "HGS Portal — Password Reset",
            HtmlBody = body
        }, cancellationToken);
    }

    public async Task ResetPasswordAsync(ResetPasswordRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Token) || string.IsNullOrWhiteSpace(request.NewPassword))
        {
            throw new BadRequestException("Token and new password are required.");
        }

        if (request.NewPassword.Length < 6)
        {
            throw new BadRequestException("Password must be at least 6 characters.");
        }

        var tokenHash = _tokenService.HashRefreshToken(request.Token);
        var user = await _dbContext.Users
            .Where(u => u.PasswordResetTokenHash == tokenHash &&
                        u.PasswordResetTokenExpiresAt != null &&
                        u.PasswordResetTokenExpiresAt > DateTime.UtcNow &&
                        !u.IsDeleted)
            .FirstOrDefaultAsync(cancellationToken);

        if (user is null)
        {
            throw new UnauthorizedException("Reset link is invalid or expired.");
        }

        user.PasswordHash = PasswordHelper.HashPassword(request.NewPassword);
        user.PasswordResetTokenHash = null;
        user.PasswordResetTokenExpiresAt = null;
        user.FailedLoginCount = 0;
        user.LockoutEnd = null;

        var activeTokens = await _dbContext.RefreshTokens
            .Where(rt => rt.UserId == user.Id && !rt.IsRevoked)
            .ToListAsync(cancellationToken);
        foreach (var token in activeTokens)
        {
            token.IsRevoked = true;
            token.RevokedAt = DateTime.UtcNow;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Password reset completed for user '{Username}'.", user.Username);

        await _auditLog.LogSecurityEventAsync(
            action: "PASSWORD_RESET_COMPLETED",
            eventCategory: "Security", success: true, severity: "Warning",
            targetUserId: user.Id, username: user.Username);
    }

    private static string GenerateResetToken()
    {
        var randomBytes = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);
        return Convert.ToBase64String(randomBytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }

    public async Task<Users?> GetCurrentUserAsync(int userId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Users
            .Include(x => x.OrganizationUnit)
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == userId && !x.IsDeleted, cancellationToken);
    }

    public void SetRefreshTokenCookie(HttpContext context, string token)
    {
        var cookieOptions = new CookieOptions
        {
            HttpOnly = true,
            Secure = _cookieSettings.Secure,
            SameSite = _cookieSettings.SameSite,
            Path = "/",
            Expires = DateTime.UtcNow.AddDays(7)
        };

        context.Response.Cookies.Append("refresh_token", token, cookieOptions);
    }

    public void ClearRefreshTokenCookie(HttpContext context)
    {
        context.Response.Cookies.Delete("refresh_token", new CookieOptions
        {
            Path = "/",
            HttpOnly = true,
            Secure = _cookieSettings.Secure,
            SameSite = _cookieSettings.SameSite
        });
    }
}
