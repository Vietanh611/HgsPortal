namespace Hgs.Share.Requests.CustomerSatisfaction;

public class DevicesUpdateRequest
{
    public string? DeviceName { get; set; }
    public string? DeviceIdentifier { get; set; }
    public string? Status { get; set; }
    public DateTime? LastSeenAt { get; set; }
}
