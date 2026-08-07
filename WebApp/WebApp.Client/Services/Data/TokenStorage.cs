using Blazored.LocalStorage;

namespace WebApp.Client.Services.Data;

public class TokenStorage : ITokenStorage
{
    private readonly ILocalStorageService _localStorageService;
    
    private const string AccessTokenKey = "accessToken";
    private const string RefreshTokenKey = "refreshToken";
    private const string ExpiresAtKey = "expiresAt";

    public TokenStorage(ILocalStorageService localStorageService)
    {
        _localStorageService = localStorageService;
    }

    public async Task<string?> GetAccessTokenAsync()
    {
        try
        {
            return await _localStorageService.GetItemAsStringAsync(AccessTokenKey);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error reading access token: {ex.Message}");
            return null;
        }
    }

    public async Task<string?> GetRefreshTokenAsync()
    {
        try
        {
            return await _localStorageService.GetItemAsStringAsync(RefreshTokenKey);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error reading refresh token: {ex.Message}");
            return null;
        }
    }

    public async Task<DateTime?> GetExpiresAtAsync()
    {
        try
        {
            var expiresAtString = await _localStorageService.GetItemAsStringAsync(ExpiresAtKey);
            if (!string.IsNullOrEmpty(expiresAtString) && DateTime.TryParse(expiresAtString, out var expiresAt))
            {
                return expiresAt;
            }
            return null;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error reading expires at: {ex.Message}");
            return null;
        }
    }

    public async Task SetTokensAsync(string accessToken, string refreshToken, DateTime expiresAt)
    {
        try
        {
            await _localStorageService.SetItemAsStringAsync(AccessTokenKey, accessToken);
            await _localStorageService.SetItemAsStringAsync(RefreshTokenKey, refreshToken);
            await _localStorageService.SetItemAsStringAsync(ExpiresAtKey, expiresAt.ToString("O"));
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error saving tokens: {ex.Message}");
        }
    }

    public async Task ClearTokensAsync()
    {
        try
        {
            await _localStorageService.RemoveItemAsync(AccessTokenKey);
            await _localStorageService.RemoveItemAsync(RefreshTokenKey);
            await _localStorageService.RemoveItemAsync(ExpiresAtKey);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error clearing tokens: {ex.Message}");
        }
    }
}
