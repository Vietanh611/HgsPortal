using System.ComponentModel.DataAnnotations;

namespace Hgs.Share.Requests.Roles;

public class RolesCreateRequest
{
    [Required(ErrorMessage = "Vui lòng nhập mã vai trò.")]
    [StringLength(50, ErrorMessage = "Mã vai trò tối đa 50 ký tự.")]
    public string Code { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập tên vai trò.")]
    [StringLength(100, ErrorMessage = "Tên vai trò tối đa 100 ký tự.")]
    public string Name { get; set; } = string.Empty;

    [StringLength(500, ErrorMessage = "Mô tả tối đa 500 ký tự.")]
    public string? Description { get; set; }

    public int? OrganizationUnitId { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn phạm vi dữ liệu.")]
    [StringLength(20, ErrorMessage = "Phạm vi dữ liệu tối đa 20 ký tự.")]
    public string DataScope { get; set; } = "Self";

    public bool IsSystemRole { get; set; }

    public bool IsActive { get; set; }
}
