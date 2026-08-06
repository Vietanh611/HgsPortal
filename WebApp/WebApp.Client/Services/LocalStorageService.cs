using Microsoft.JSInterop;

namespace WebApp.Client.Services;

public class LocalStorageService
{
    private readonly IJSRuntime _jsRuntime;
    
    public const string AccessTokenKey = "accessToken";
    public const string RefreshTokenKey = "refreshToken";
    public const string ExpiresAtKey = "expiresAt";

    public LocalStorageService(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    public async Task<string?> GetAccessTokenAsync()
    {
        try
        {
            return await _jsRuntime.InvokeAsync<string?>("localStorage.getItem", AccessTokenKey);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error reading access token: {ex.Message}");
            return null;
        }
    }

    public async Task SetTokensAsync(string accessToken, string refreshToken, string? expiresAt = null)
    {
        try
        {
            await _jsRuntime.InvokeVoidAsync("localStorage.setItem", AccessTokenKey, accessToken);
            await _jsRuntime.InvokeVoidAsync("localStorage.setItem", RefreshTokenKey, refreshToken);
            if (!string.IsNullOrEmpty(expiresAt))
            {
                await _jsRuntime.InvokeVoidAsync("localStorage.setItem", ExpiresAtKey, expiresAt);
            }
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
            await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", AccessTokenKey);
            await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", RefreshTokenKey);
            await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", ExpiresAtKey);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error clearing tokens: {ex.Message}");
        }
    }
}
