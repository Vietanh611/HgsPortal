namespace Hgs.Share.Responses.Devices;

public class DeviceGetAllResponse
{
    public int Id { get; set; }
    public string DeviceType { get; set; } = string.Empty;
    public string DeviceName { get; set; } = string.Empty;
    public string DeviceIdentifier { get; set; } = string.Empty;
    public int? OrganizationUnitId { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime? LastSeenAt { get; set; }
    public bool IsEnabled { get; set; }
}
