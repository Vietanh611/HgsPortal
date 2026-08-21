using Domain.Entities.DeviceManagement;

namespace Core.Interfaces.Operations;

/// <summary>
/// Lý do xác thực thiết bị (DeviceKey) bị từ chối. Kiosk dùng thông tin này để quyết định
/// giữ hay xóa cấu hình ghép cặp: bị tắt (Disabled) vẫn có thể tự hồi phục khi admin bật lại,
/// còn bị thu hồi (Revoked) buộc phải ghép cặp lại bằng mã mới.
/// </summary>
public enum DeviceAuthFailureReason
{
    /// <summary>Xác thực thành công (Device != null).</summary>
    None = 0,
    /// <summary>Thiết bị không tồn tại, đã xóa, đang PENDING, sai key hoặc sai loại thiết bị.</summary>
    Invalid,
    /// <summary>Thiết bị tồn tại nhưng đang bị admin tắt (IsEnabled = false).</summary>
    Disabled,
    /// <summary>Thiết bị đã bị thu hồi (Status = REVOKED).</summary>
    Revoked
}

/// <summary>
/// Kết quả của <see cref="IDevicesService.AuthenticateDeviceAsync"/>: kèm lý do bị từ chối
/// (Disabled/Revoked) để caller phân biệt các trạng thái khi khóa thiết bị hết hiệu lực.
/// </summary>
public sealed record DeviceAuthenticationResult(Device? Device, DeviceAuthFailureReason Reason)
{
    /// <summary>True khi xác thực thành công (có device hợp lệ).</summary>
    public bool IsAuthenticated => Device is not null && Reason == DeviceAuthFailureReason.None;
}