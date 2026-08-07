using Domain.Entities.Identity;
using Hgs.Share.Requests;
using Hgs.Share.Requests.Users;
using Hgs.Share.Responses;
using Microsoft.AspNetCore.Http;

namespace Core.Interfaces;

public interface IAuthService
{
    Task<AuthenticateResponse> LoginAsync(
        AuthenticateRequest request,
        string? userAgent,
        string? ipAddress,
        CancellationToken cancellationToken = default);

    Task<AuthenticateResponse> RefreshTokenAsync(
        RefreshTokenRequest request,
        string? userAgent,
        string? ipAddress,
        CancellationToken cancellationToken = default);

    Task LogoutAsync(
        LogoutRequest request,
        CancellationToken cancellationToken = default);

    Task<Users?> GetCurrentUserAsync(
        int userId,
        CancellationToken cancellationToken = default);

    void SetRefreshTokenCookie(HttpContext context, string token);
    void ClearRefreshTokenCookie(HttpContext context);
}
