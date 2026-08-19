using Hgs.Share.Requests;
using Hgs.Share.Responses;
using Hgs.Share.Responses.ApiResponses;
using Microsoft.AspNetCore.Components;
using System.Net.Http.Json;
using System.Text.Json;

namespace WebApp.Client.Services.Auth;

/// <summary>
/// Service xác thực phía client: gọi API auth (login/logout/refresh) và lưu access token
/// vào storage. Logout luôn xóa token cục bộ kể cả khi API lỗi.
/// </summary>
public class AuthService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly Data.ITokenStorage _tokenStorage;
    private readonly NavigationManager _navigationManager;
    private readonly TokenRefreshService _tokenRefreshService;
    private readonly JsonSerializerOptions _jsonOptions;

    public AuthService(
        IHttpClientFactory httpClientFactory,
        Data.ITokenStorage tokenStorage,
        NavigationManager navigationManager,
        TokenRefreshService tokenRefreshService)
    {
        _httpClientFactory = httpClientFactory;
        _tokenStorage = tokenStorage;
        _navigationManager = navigationManager;
        _tokenRefreshService = tokenRefreshService;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }

    private HttpClient CreateHttpClient()
    {
        return _httpClientFactory.CreateClient("AuthClient");
    }

    /// <summary>
    /// Đăng nhập; khi thành công, lưu access token và thời gian hết hạn vào storage
    /// để các handler dùng cho những request tiếp theo. Trả về envelope <see cref="ApiResponse{T}"/>
    /// để caller nhận được ErrorCode/Message khi thất bại (ví dụ "ACCOUNT_LOCKED" — tài khoản
    /// bị khóa do đăng nhập sai nhiều lần) thay vì chỉ nhận null.
    /// </summary>
    public async Task<ApiResponse<AuthenticateResponse>?> LoginAsync(string username, string password)
    {
        try
        {
            var request = new AuthenticateRequest { Username = username, Password = password };
            var response = await CreateHttpClient().PostAsJsonAsync("auth/login", request, _jsonOptions);

            var content = await response.Content.ReadAsStringAsync();
            var apiResponse = JsonSerializer.Deserialize<ApiResponse<AuthenticateResponse>>(content, _jsonOptions);

            if (apiResponse is null)
            {
                Console.WriteLine($"Login failed: {response.StatusCode} - {(string.IsNullOrWhiteSpace(content) ? "(empty)" : content)}");
                return response.IsSuccessStatusCode
                    ? null
                    : ApiResponse<AuthenticateResponse>.FailResponse("Đăng nhập thất bại, vui lòng thử lại.", (int)response.StatusCode);
            }

            if (apiResponse.Success && apiResponse.Data != null)
            {
                await _tokenStorage.SetAccessTokenAsync(
                    apiResponse.Data.AccessToken,
                    apiResponse.Data.ExpiresAt);
            }

            return apiResponse;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Login error: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Đăng xuất: gọi API logout (lỗi thì bỏ qua) rồi luôn xóa token cục bộ và quay về
    /// trang đăng nhập, trừ khi đang ở trang công cộng.
    /// </summary>
    public async Task LogoutAsync()
    {
        try
        {
            await CreateHttpClient().PostAsync("auth/logout", null!);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Logout API error: {ex.Message}");
        }
        finally
        {
            await _tokenStorage.ClearTokensAsync();
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
        }
    }

    /// <summary>
    /// Refresh access token dựa trên refresh cookie và lưu token mới; trả về false khi thất
    /// bại để caller quyết định xử lý (ví dụ chuyển sang trạng thái anonymous).
    /// Đi qua <see cref="TokenRefreshService"/> để mọi lần refresh trong phiên dùng chung một
    /// khóa — chống 2 request song song cùng gửi refresh cookie cũ (race gây reuse detection).
    /// </summary>
    public async Task<bool> RefreshAccessTokenAsync()
    {
        try
        {
            return await _tokenRefreshService.RefreshTokenAsync() == TokenRefreshResult.Success;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Token refresh error: {ex.Message}");
            return false;
        }
    }
}
