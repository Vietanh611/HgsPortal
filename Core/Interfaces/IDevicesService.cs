using Domain.Entities.DeviceManagement;
using Hgs.Share.Requests.Devices;
using Hgs.Share.Responses.Devices;

namespace Core.Interfaces;

public interface IDevicesService
{
    /// <summary>Tạo row Devices mới (Status=PENDING), sinh mã pairing ngắn hạn. Trả plaintext mã 1 lần.</summary>
    Task<DevicePairingCodeCreateResponse> CreatePairingCodeAsync(DevicePairingCodeCreateRequest request, int? createdBy, CancellationToken cancellationToken = default);

    Task<IEnumerable<DeviceGetAllResponse>> GetAllAsync(
        string? status,
        int? organizationUnitId,
        CancellationToken cancellationToken = default);

    Task<DeviceGetByIdResponse?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<DeviceStatusUpdateResponse> UpdateStatusAsync(int id, bool isEnabled, int? updatedBy, CancellationToken cancellationToken = default);

    Task<DeviceRevokeResponse> RevokeAsync(int id, int? revokedBy, CancellationToken cancellationToken = default);

    /// <summary>Tạo lại mã pairing cho thiết bị (reset về PENDING, xoá DeviceKeyHash cũ).</summary>
    Task<DevicePairingCodeRegenerateResponse> RegeneratePairingCodeAsync(int id, int? requestedBy, CancellationToken cancellationToken = default);

    Task DeleteAsync(int id, int? deletedBy, CancellationToken cancellationToken = default);

    /// <summary>Ghép cặp thiết bị bằng mã pairing (Anonymous - kiosk). Trả { deviceId, deviceKey } 1 lần duy nhất.</summary>
    Task<DevicePairResponse> PairDeviceAsync(string pairingCode, string? ipAddress, CancellationToken cancellationToken = default);

    /// <summary>Xác thực thiết bị bằng DeviceIdentifier + DeviceKey. Trả về device nếu hợp lệ, ngược lại null.</summary>
    Task<Device?> AuthenticateDeviceAsync(string deviceIdentifier, string deviceKey, string? expectedDeviceType = null, CancellationToken cancellationToken = default);

    /// <summary>Cập nhật LastSeenAt/LastSeenIp (gọi khi thiết bị thực hiện request hợp lệ).</summary>
    Task UpdateLastSeenAtAsync(int deviceId, string? ipAddress, CancellationToken cancellationToken = default);
}
