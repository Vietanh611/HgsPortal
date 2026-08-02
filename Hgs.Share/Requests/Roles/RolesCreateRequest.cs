using System.ComponentModel.DataAnnotations;

namespace Hgs.Share.Requests.Roles;

public class RolesCreateRequest
{
    [Required]
    [StringLength(50)]
    public string Code { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Description { get; set; }

    public int? OrganizationUnitId { get; set; }

    [Required]
    [StringLength(20)]
    public string DataScope { get; set; } = "Self";

    public bool IsSystemRole { get; set; }

    public bool IsActive { get; set; }
}
