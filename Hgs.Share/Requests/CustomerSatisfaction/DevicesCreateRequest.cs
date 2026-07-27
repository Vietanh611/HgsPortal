namespace Hgs.Share.Requests.CustomerSatisfaction;

public class DevicesCreateRequest
{
    public string DeviceName { get; set; } = string.Empty;
    public string DeviceIdentifier { get; set; } = string.Empty;
    public string? Status { get; set; }
    public DateTime? LastSeenAt { get; set; }
}
