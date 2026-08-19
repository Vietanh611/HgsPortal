namespace WebApp.Client.Services.Data;

/// <summary>
/// Hợp đồng lưu trữ access token phía client, có triển khai khác nhau giữa WebAssembly
/// (localStorage) và prerendering phía server (no-op, vì token chỉ tồn tại ở client).
/// </summary>
public interface ITokenStorage
{
    /// <summary>
    /// True khi instance này là ServerTokenStorage của server-side prerender, vốn là no-op
    /// không đọc được token thật. Ứng dụng chỉ chạy InteractiveWebAssembly nên component
    /// code phía server chỉ chạy đúng giai đoạn prerender — marker này cho phép base class
    /// (AuthorizedPageBase) phân biệt prerender với lúc đã interactive để bỏ qua data-loading
    /// (API trả 401 khi thiếu token và NavigateTo bị cấm trong lúc prerender).
    /// </summary>
    bool IsServerSidePrerender { get; }

    Task<string?> GetAccessTokenAsync();
    Task<DateTime?> GetExpiresAtAsync();
    Task SetAccessTokenAsync(string accessToken, DateTime expiresAt);
    Task ClearTokensAsync();
}
