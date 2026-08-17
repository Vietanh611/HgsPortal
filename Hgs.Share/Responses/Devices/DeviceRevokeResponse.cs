namespace Hgs.Share.Responses.Devices;

public class DeviceRevokeResponse
{
    public int Id { get; set; }
    public string DeviceType { get; set; } = string.Empty;
    public string DeviceName { get; set; } = string.Empty;
    public string DeviceIdentifier { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime? RevokedAt { get; set; }
}
