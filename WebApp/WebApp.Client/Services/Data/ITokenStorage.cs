namespace WebApp.Client.Services.Data;

public interface ITokenStorage
{
    Task<string?> GetAccessTokenAsync();
    Task<DateTime?> GetExpiresAtAsync();
    Task SetAccessTokenAsync(string accessToken, DateTime expiresAt);
    Task ClearTokensAsync();
}
