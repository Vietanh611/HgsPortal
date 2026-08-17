using System.ComponentModel.DataAnnotations;

namespace Hgs.Share.Requests.Devices;

public class DevicePairRequest
{
    [Required(ErrorMessage = "Vui lòng nhập mã pairing.")]
    [StringLength(8, MinimumLength = 8, ErrorMessage = "Mã pairing gồm đúng 8 ký tự.")]
    public string PairingCode { get; set; } = string.Empty;
}
