namespace WebApp.Client.Services.Data;

public interface ITokenStorage
{
    Task<string?> GetAccessTokenAsync();
    Task<string?> GetRefreshTokenAsync();
    Task<DateTime?> GetExpiresAtAsync();
    Task SetTokensAsync(string accessToken, string refreshToken, DateTime expiresAt);
    Task ClearTokensAsync();
}
