using Hgs.Share.Responses;
using Hgs.Share.Responses.ApiResponses;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using System.Security.Claims;

namespace WebApp.Client.Services.Auth;

/// <summary>
/// Cung cấp AuthenticationState cho Blazor từ token đã lưu: token sắp hết hạn (trong
/// 5 phút) sẽ được refresh trước; khi chưa đăng nhập hoặc refresh thất bại, trả về trạng
/// thái anonymous thay vì ném lỗi làm hỏng quá trình render.
/// </summary>
public class CustomAuthenticationStateProvider : AuthenticationStateProvider
{
    private readonly Data.ITokenStorage _tokenStorage;
    private readonly Data.JwtTokenService _jwtTokenService;
    private readonly AuthService _authService;
    private readonly TokenRefreshService _tokenRefreshService;
    private readonly NavigationManager _navigationManager;

    public CustomAuthenticationStateProvider(
        Data.ITokenStorage tokenStorage,
        Data.JwtTokenService jwtTokenService,
        AuthService authService,
        TokenRefreshService tokenRefreshService,
        NavigationManager navigationManager)
    {
        _tokenStorage = tokenStorage;
        _jwtTokenService = jwtTokenService;
        _authService = authService;
        _tokenRefreshService = tokenRefreshService;
        _navigationManager = navigationManager;
    }

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        try
        {
            var token = await _tokenStorage.GetAccessTokenAsync();
            var expiresAt = await _tokenStorage.GetExpiresAtAsync();

            // Check if token is expired or will expire soon (within 5 minutes)
            if (!string.IsNullOrEmpty(token) && expiresAt.HasValue && expiresAt.Value <= DateTime.UtcNow.AddMinutes(5))
            {
                Console.WriteLine("Access token expired or expiring soon, attempting refresh...");
                var refreshResult = await _authService.RefreshAccessTokenAsync();

                if (refreshResult)
                {
                    token = await _tokenStorage.GetAccessTokenAsync();
                    expiresAt = await _tokenStorage.GetExpiresAtAsync();
                    Console.WriteLine("Token refreshed successfully");
                }
                else
                {
                    Console.WriteLine("Token refresh failed, clearing tokens");
                    await _tokenStorage.ClearTokensAsync();
                    return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
                }
            }

            if (string.IsNullOrEmpty(token))
            {
                return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
            }

            var userId = _jwtTokenService.ExtractUserId(token);
            if (!userId.HasValue)
            {
                return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
            }

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId.Value.ToString()),
                new Claim(ClaimTypes.Name, userId.Value.ToString())
            };

            var identity = new ClaimsIdentity(claims, "jwt");
            var principal = new ClaimsPrincipal(identity);

            return new AuthenticationState(principal);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error getting authentication state: {ex.Message}");
            return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
        }
    }

    public async Task<ApiResponse<AuthenticateResponse>?> LoginAsync(string username, string password)
    {
        var result = await _authService.LoginAsync(username, password);

        if (result?.Success == true)
        {
            //var authState = await GetAuthenticationStateAsync();
            //NotifyAuthenticationStateChanged(Task.FromResult(authState));
        }

        return result;
    }

    /// <summary>
    /// Đăng xuất rồi thông báo Blazor cập nhật trạng thái đăng nhập (chuyển về anonymous).
    /// </summary>
    public async Task LogoutAsync()
    {
        await _authService.LogoutAsync();
        NotifyAuthenticationStateChanged();
    }

    /// <summary>
    /// Thông báo cho Blazor rằng trạng thái đăng nhập đã thay đổi, buộc các component
    /// phụ thuộc xác thực render lại.
    /// </summary>
    public void NotifyAuthenticationStateChanged()
    {
        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
    }
}
