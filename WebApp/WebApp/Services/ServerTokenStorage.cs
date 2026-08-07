using WebApp.Client.Services.Data;

namespace WebApp.Services;

/// <summary>
/// Server-side implementation of ITokenStorage for prerendering.
/// This is a no-op implementation since tokens are only stored on the client side.
/// </summary>
public class ServerTokenStorage : WebApp.Client.Services.Data.ITokenStorage
{
    public Task<string?> GetAccessTokenAsync()
    {
        return Task.FromResult<string?>(null);
    }

    public Task<DateTime?> GetExpiresAtAsync()
    {
        return Task.FromResult<DateTime?>(null);
    }

    public Task SetAccessTokenAsync(string accessToken, DateTime expiresAt)
    {
        // No-op on server side
        return Task.CompletedTask;
    }

    public Task ClearTokensAsync()
    {
        // No-op on server side
        return Task.CompletedTask;
    }
}
