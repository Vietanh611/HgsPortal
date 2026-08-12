namespace Hgs.Share.Responses.DisplayDevices;

public class DisplayDevicesGetByIdResponse
{
    public int Id { get; set; }
    public string DeviceName { get; set; } = string.Empty;
    public string DeviceIdentifier { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime? LastSeenAt { get; set; }
    public bool IsEnabled { get; set; }
}