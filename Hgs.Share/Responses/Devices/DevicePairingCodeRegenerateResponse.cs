namespace Hgs.Share.Responses.Devices;

public class DevicePairingCodeRegenerateResponse
{
    public int DeviceRowId { get; set; }
    public string DeviceIdentifier { get; set; } = string.Empty;
    public string DeviceName { get; set; } = string.Empty;
    /// <summary>Mã pairing mới - chỉ trả về đúng 1 lần lúc tạo lại.</summary>
    public string PairingCode { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
}
