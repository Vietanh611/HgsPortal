using System.ComponentModel.DataAnnotations;

namespace Hgs.Share.Requests.Menus;

public class MenusCreateRequest
{

    public int? ParentId { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập mã menu.")]
    [MaxLength(100, ErrorMessage = "Mã menu tối đa 100 ký tự.")]
    public string Code { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập tên menu.")]
    [MaxLength(200, ErrorMessage = "Tên menu tối đa 200 ký tự.")]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500, ErrorMessage = "Đường dẫn tối đa 500 ký tự.")]
    public string? Route { get; set; }

    [MaxLength(200, ErrorMessage = "Component tối đa 200 ký tự.")]
    public string? Component { get; set; }

    [MaxLength(100, ErrorMessage = "Icon tối đa 100 ký tự.")]
    public string? Icon { get; set; }

    public int SortOrder { get; set; }

    public bool IsVisible { get; set; } = true;

    public bool IsActive { get; set; } = true;
}
