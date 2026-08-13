using Hgs.Share.Requests;
using Hgs.Share.Responses;
using Hgs.Share.Responses.ApiResponses;
using Microsoft.AspNetCore.Components;
using System.Net.Http.Json;
using System.Text.Json;

namespace WebApp.Client.Services.Auth;

public class AuthService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly Data.ITokenStorage _tokenStorage;
    private readonly NavigationManager _navigationManager;
    private readonly JsonSerializerOptions _jsonOptions;

    public AuthService(
        IHttpClientFactory httpClientFactory,
        Data.ITokenStorage tokenStorage,
        NavigationManager navigationManager)
    {
        _httpClientFactory = httpClientFactory;
        _tokenStorage = tokenStorage;
        _navigationManager = navigationManager;
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

    public async Task<AuthenticateResponse?> LoginAsync(string username, string password)
    {
        try
        {
            var request = new AuthenticateRequest { Username = username, Password = password };
            var response = await CreateHttpClient().PostAsJsonAsync("auth/login", request, _jsonOptions);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Login failed: {response.StatusCode} - {errorContent}");
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
            Console.WriteLine($"Login error: {ex.Message}");
            return null;
        }
    }

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

    public async Task<bool> RefreshAccessTokenAsync()
    {
        try
        {
            var response = await CreateHttpClient().PostAsync("auth/refresh-token", null!);

            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine($"Token refresh failed: {response.StatusCode}");
                return false;
            }

            var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<AuthenticateResponse>>(_jsonOptions);
            if (apiResponse?.Success == true && apiResponse.Data != null)
            {
                await _tokenStorage.SetAccessTokenAsync(
                    apiResponse.Data.AccessToken,
                    apiResponse.Data.ExpiresAt);
                return true;
            }

            return false;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Token refresh error: {ex.Message}");
            return false;
        }
    }
}
