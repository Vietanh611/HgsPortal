using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using WebApp.Client.Services.Auth;

namespace WebApp.Client.Services.Network;

/// <summary>
/// Trước mỗi request, tự động refresh access token nếu token sắp hết hạn (trong 5 phút)
/// để phiên đăng nhập không bị gián đoạn. Nếu refresh thất bại, xóa token, thông báo thay
/// đổi trạng thái đăng nhập và chuyển về trang đăng nhập (trừ các trang công cộng).
/// </summary>
public class TokenRefreshHandler : DelegatingHandler
{
    private readonly Data.ITokenStorage _tokenStorage;
    private readonly Auth.TokenRefreshService _tokenRefreshService;
    private readonly AuthenticationStateProvider _authenticationStateProvider;
    private readonly NavigationManager _navigationManager;
    
    private static readonly TimeSpan RefreshThreshold = TimeSpan.FromMinutes(5);

    public TokenRefreshHandler(
        Data.ITokenStorage tokenStorage,
        Auth.TokenRefreshService tokenRefreshService,
        AuthenticationStateProvider authenticationStateProvider,
        NavigationManager navigationManager)
    {
        _tokenStorage = tokenStorage;
        _tokenRefreshService = tokenRefreshService;
        _authenticationStateProvider = authenticationStateProvider;
        _navigationManager = navigationManager;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        try
        {
            var accessToken = await _tokenStorage.GetAccessTokenAsync();
            var expiresAt = await _tokenStorage.GetExpiresAtAsync();

            if (!string.IsNullOrEmpty(accessToken) && expiresAt.HasValue)
            {
                var timeUntilExpiry = expiresAt.Value - DateTime.UtcNow;
                
                if (timeUntilExpiry <= RefreshThreshold)
                {
                    Console.WriteLine($"Token expiring in {timeUntilExpiry.TotalMinutes:F1} minutes, refreshing...");
                    
                    var refreshResult = await _tokenRefreshService.RefreshTokenAsync();
                    
                    // Chỉ đăng xuất khi refresh cookie thực sự không còn hợp lệ (phiên chết).
                    // Lỗi mạng/server tạm thời (NetworkError): giữ phiên, cứ gửi request — nếu
                    // server trả 401 thì ApiClient (silent) sẽ bỏ qua, request tương tác xử lý sau.
                    if (refreshResult == TokenRefreshResult.SessionExpired)
                    {
                        Console.WriteLine("Token refresh failed, logging out");
                        await _tokenStorage.ClearTokensAsync();

                        if (_authenticationStateProvider is Auth.CustomAuthenticationStateProvider customProvider)
                        {
                            customProvider.NotifyAuthenticationStateChanged();
                        }

                        var currentUri = _navigationManager.Uri;
                        var loginUri = _navigationManager.ToAbsoluteUri("/login").ToString();
                        var rootUri = _navigationManager.ToAbsoluteUri("/").ToString();
                        var domesticDisplayUri = _navigationManager.ToAbsoluteUri("/display/DomesticBaggageArrivalDisplay").ToString();
                        var internationalDisplayUri = _navigationManager.ToAbsoluteUri("/display/InternationalBaggageArrivalDisplay").ToString();

                        // Don't redirect to login if on display pages (public pages)
                        if (!string.Equals(currentUri, loginUri, StringComparison.OrdinalIgnoreCase) &&
                            !string.Equals(currentUri, rootUri, StringComparison.OrdinalIgnoreCase) &&
                            !string.Equals(currentUri, domesticDisplayUri, StringComparison.OrdinalIgnoreCase) &&
                            !string.Equals(currentUri, internationalDisplayUri, StringComparison.OrdinalIgnoreCase))
                        {
                            _navigationManager.NavigateTo("login", forceLoad: true);
                        }

                        return new HttpResponseMessage(System.Net.HttpStatusCode.Unauthorized);
                    }
                    
                    if (refreshResult == TokenRefreshResult.Success)
                    {
                        Console.WriteLine("Token refreshed successfully");
                    }
                    else
                    {
                        Console.WriteLine("Token refresh failed (network error) - sending request as-is");
                    }
                }
            }

            return await base.SendAsync(request, cancellationToken);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in TokenRefreshHandler: {ex.Message}");
            return await base.SendAsync(request, cancellationToken);
        }
    }
}
