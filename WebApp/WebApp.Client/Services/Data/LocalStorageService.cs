using Microsoft.JSInterop;

namespace WebApp.Client.Services.Data;

public class LocalStorageService
{
    private readonly IJSRuntime _jsRuntime;

    public LocalStorageService(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    public async Task<string?> GetItemAsync(string key)
    {
        try
        {
            return await _jsRuntime.InvokeAsync<string>("localStorage.getItem", key);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error getting item from localStorage: {ex.Message}");
            return null;
        }
    }

    public async Task SetItemAsync(string key, string value)
    {
        try
        {
            await _jsRuntime.InvokeVoidAsync("localStorage.setItem", key, value);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error setting item in localStorage: {ex.Message}");
        }
    }

    public async Task RemoveItemAsync(string key)
    {
        try
        {
            await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", key);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error removing item from localStorage: {ex.Message}");
        }
    }

    public async Task ClearAsync()
    {
        try
        {
            await _jsRuntime.InvokeVoidAsync("localStorage.clear");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error clearing localStorage: {ex.Message}");
        }
    }
}
