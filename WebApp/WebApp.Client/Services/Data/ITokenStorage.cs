namespace WebApp.Client.Services.Data;

/// <summary>
/// Hợp đồng lưu trữ access token phía client, có triển khai khác nhau giữa WebAssembly
/// (localStorage) và prerendering phía server (no-op, vì token chỉ tồn tại ở client).
/// </summary>
public interface ITokenStorage
{
    Task<string?> GetAccessTokenAsync();
    Task<DateTime?> GetExpiresAtAsync();
    Task SetAccessTokenAsync(string accessToken, DateTime expiresAt);
    Task ClearTokensAsync();
}
