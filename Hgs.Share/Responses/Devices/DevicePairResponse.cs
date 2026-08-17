namespace Hgs.Share.Responses.Devices;

public class DevicePairResponse
{
    public string DeviceId { get; set; } = string.Empty;
    public string DeviceType { get; set; } = string.Empty;
    public string DeviceName { get; set; } = string.Empty;
    /// <summary>DeviceKey dạng plaintext - chỉ trả về đúng 1 lần tại thời điểm pairing.</summary>
    public string DeviceKey { get; set; } = string.Empty;
}
