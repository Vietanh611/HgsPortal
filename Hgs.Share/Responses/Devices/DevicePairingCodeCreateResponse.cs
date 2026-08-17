namespace Hgs.Share.Responses.Devices;

public class DevicePairingCodeCreateResponse
{
    public int DeviceRowId { get; set; }
    public string DeviceIdentifier { get; set; } = string.Empty;
    public string DeviceName { get; set; } = string.Empty;
    public string DeviceType { get; set; } = string.Empty;
    public int? OrganizationUnitId { get; set; }
    /// <summary>Mã pairing dạng plaintext - chỉ trả về đúng 1 lần lúc tạo.</summary>
    public string PairingCode { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
}
