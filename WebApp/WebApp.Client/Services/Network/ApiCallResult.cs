namespace WebApp.Client.Services.Network;

/// <summary>
/// Kết quả của một request API nền (silent) dùng <see cref="ApiClient.GetSilentAsync{T}"/>.
/// Không tự redirect, không retry — caller tự quyết định xử lý dựa trên cờ trạng thái:
/// 401 (IsUnauthorized) → refresh + thử lại; 403 (IsForbidden) → bỏ qua; lỗi khác
/// (ErrorMessage) → thử lại chu kỳ sau.
/// </summary>
public class ApiCallResult<T>
{
    public bool Success { get; init; }
    public T? Data { get; init; }
    public bool IsUnauthorized { get; init; }
    public bool IsForbidden { get; init; }
    public string? ErrorMessage { get; init; }
}
