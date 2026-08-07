using Hgs.Share.Responses;
using Hgs.Share.Responses.ApiResponses;
using System.Net.Http.Json;
using System.Text.Json;

namespace WebApp.Client.Services.Auth;

public class TokenRefreshService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly Data.ITokenStorage _tokenStorage;
    private readonly JsonSerializerOptions _jsonOptions;

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

    public async Task<AuthenticateResponse?> RefreshTokenAsync()
    {
        try
        {
            var response = await CreateHttpClient().PostAsync("auth/refresh-token", null!);

            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine($"Token refresh failed: {response.StatusCode}");
                return null;
            }

            var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<AuthenticateResponse>>(_jsonOptions);
            if (apiResponse?.Success == true && apiResponse.Data != null)
            {
                await _tokenStorage.SetAccessTokenAsync(
                    apiResponse.Data.AccessToken,
                    apiResponse.Data.ExpiresAt);
                return apiResponse.Data;
            }

            return null;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Token refresh error: {ex.Message}");
            return null;
        }
    }
}
