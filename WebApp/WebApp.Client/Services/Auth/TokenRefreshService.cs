using Hgs.Share.Responses;
using Hgs.Share.Responses.ApiResponses;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;

namespace WebApp.Client.Services.Auth;

/// <summary>
/// Gọi API refresh-token (dùng refresh cookie HttpOnly) để cấp access token mới và lưu
/// lại vào storage; được các handler gọi trước request nhằm tránh token hết hạn.
/// </summary>
public class TokenRefreshService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly Data.ITokenStorage _tokenStorage;
    private readonly JsonSerializerOptions _jsonOptions;

    /// <summary>
    /// Khóa dùng chung cho MỌI lần refresh trong phiên (static) — chống 2 request song song
    /// cùng gửi refresh cookie cũ. Server rotate refresh token ngay khi refresh (AuthService
    /// server revoke token cũ), nên nếu 2 request cùng refresh thì request thua gửi token đã
    /// revoke → server báo reuse → thu hồi phiên → bị đá ra trang đăng nhập. Đây chính là lỗi
    /// "nhận notification thì bị quay ra login": bell poll 2 request song song mỗi 60 giây.
    /// </summary>
    private static readonly SemaphoreSlim RefreshGate = new(1, 1);

    private static readonly TimeSpan RefreshThreshold = TimeSpan.FromMinutes(5);

    public TokenRefreshService(
        IHttpClientFactory httpClientFactory,
        Data.ITokenStorage tokenStorage)
    {
        _httpClientFactory = httpClientFactory;
        _tokenStorage = tokenStorage;
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
    /// Refresh access token và lưu token mới; trả về <see cref="TokenRefreshResult"/> để caller
    /// phân biệt phiên hết hạn thật (phải đăng xuất) với lỗi mạng tạm thời (giữ phiên, thử lại).
    /// Không ném exception ra ngoài.
    /// </summary>
    public async Task<TokenRefreshResult> RefreshTokenAsync()
    {
        await RefreshGate.WaitAsync();
        try
        {
            // Đã có một refresh khác vừa hoàn tất trong lúc ta chờ lock (request song song):
            // token trong storage đã tươi — bỏ qua gọi API. Nếu cứ gọi tiếp sẽ dùng refresh
            // cookie cũ vừa bị rotate/revoke → server phát hiện reuse → thu hồi phiên.
            var expiresAt = await _tokenStorage.GetExpiresAtAsync();
            if (expiresAt.HasValue && expiresAt.Value > DateTime.UtcNow.Add(RefreshThreshold))
            {
                var token = await _tokenStorage.GetAccessTokenAsync();
                return string.IsNullOrEmpty(token)
                    ? TokenRefreshResult.SessionExpired
                    : TokenRefreshResult.Success;
            }

            return await RefreshCoreAsync();
        }
        finally
        {
            RefreshGate.Release();
        }
    }

    private async Task<TokenRefreshResult> RefreshCoreAsync()
    {
        try
        {
            var response = await CreateHttpClient().PostAsync("auth/refresh-token", null!);

            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine($"Token refresh failed: {response.StatusCode}");

                // 400/401 = refresh cookie không còn hợp lệ (revoke/hết hạn) → phiên đã chết.
                // 5xx/429 = lỗi server tạm thời → giữ phiên, thử lại chu kỳ sau.
                var status = (int)response.StatusCode;
                return status is 400 or 401
                    ? TokenRefreshResult.SessionExpired
                    : TokenRefreshResult.NetworkError;
            }

            var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<AuthenticateResponse>>(_jsonOptions);
            if (apiResponse?.Success == true && apiResponse.Data != null)
            {
                await _tokenStorage.SetAccessTokenAsync(
                    apiResponse.Data.AccessToken,
                    apiResponse.Data.ExpiresAt);
                return TokenRefreshResult.Success;
            }

            // 2xx nhưng không parse được token — phòng thủ: coi như phiên không còn xác thực.
            return TokenRefreshResult.SessionExpired;
        }
        catch (HttpRequestException ex)
        {
            Console.WriteLine($"Token refresh network error: {ex.Message}");
            return TokenRefreshResult.NetworkError;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Token refresh error: {ex.Message}");
            return TokenRefreshResult.SessionExpired;
        }
    }
}
