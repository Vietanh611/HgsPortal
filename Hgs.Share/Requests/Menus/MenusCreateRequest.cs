using System.ComponentModel.DataAnnotations;

namespace Hgs.Share.Requests.Menus;

public class MenusCreateRequest
{
    [Required]
    public int ModuleId { get; set; }

    public int? ParentId { get; set; }

    [Required]
    [MaxLength(100)]
    public string Code { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Route { get; set; }

    [MaxLength(200)]
    public string? Component { get; set; }

    [MaxLength(100)]
    public string? Icon { get; set; }

    public int SortOrder { get; set; }

    public bool IsVisible { get; set; } = true;

    public bool IsActive { get; set; } = true;
}
