namespace WebApp.Client.Services.Data;

public class MemoryTokenStorage : ITokenStorage
{
    private string? _accessToken;
    private DateTime? _expiresAt;

    public Task<string?> GetAccessTokenAsync()
    {
        return Task.FromResult(_accessToken);
    }

    public Task<DateTime?> GetExpiresAtAsync()
    {
        return Task.FromResult(_expiresAt);
    }

    public Task SetAccessTokenAsync(string accessToken, DateTime expiresAt)
    {
        _accessToken = accessToken;
        _expiresAt = expiresAt;
        return Task.CompletedTask;
    }

    public Task ClearTokensAsync()
    {
        _accessToken = null;
        _expiresAt = null;
        return Task.CompletedTask;
    }
}
