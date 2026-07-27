namespace Hgs.Share.Responses.CustomerSatisfaction;

public class DevicesGetAllResponse
{
    public int Id { get; set; }
    public string DeviceName { get; set; } = string.Empty;
    public string DeviceIdentifier { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime? LastSeenAt { get; set; }
}
