namespace Hgs.Share.Requests.Roles;

public class RolesUpdateRequest
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int? OrganizationUnitId { get; set; }
    public string DataScope { get; set; } = "Self";
    public bool IsSystemRole { get; set; }
    public bool IsActive { get; set; }
}
