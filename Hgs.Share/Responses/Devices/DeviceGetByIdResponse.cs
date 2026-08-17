namespace Hgs.Share.Responses.Devices;

public class DeviceGetByIdResponse
{
    public int Id { get; set; }
    public string DeviceType { get; set; } = string.Empty;
    public string DeviceName { get; set; } = string.Empty;
    public string DeviceIdentifier { get; set; } = string.Empty;
    public int? OrganizationUnitId { get; set; }
    public bool IsEnabled { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime? RevokedAt { get; set; }
    public string? RevokedByUserName { get; set; }
    public DateTime? LastSeenAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public int? CreatedBy { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
}
