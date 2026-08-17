namespace Hgs.Share.Responses.Devices;

public class DeviceStatusUpdateResponse
{
    public int Id { get; set; }
    public string DeviceType { get; set; } = string.Empty;
    public string DeviceName { get; set; } = string.Empty;
    public string DeviceIdentifier { get; set; } = string.Empty;
    public bool IsEnabled { get; set; }
    public string Status { get; set; } = string.Empty;
}
