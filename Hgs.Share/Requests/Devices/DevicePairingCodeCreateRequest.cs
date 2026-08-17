using System.ComponentModel.DataAnnotations;

namespace Hgs.Share.Requests.Devices;

public class DevicePairingCodeCreateRequest
{
    [Required(ErrorMessage = "Vui lòng nhập tên thiết bị.")]
    [StringLength(200, ErrorMessage = "Tên thiết bị tối đa 200 ký tự.")]
    public string DeviceName { get; set; } = string.Empty;

    public string? DeviceType { get; set; }

    public int? OrganizationUnitId { get; set; }
}
