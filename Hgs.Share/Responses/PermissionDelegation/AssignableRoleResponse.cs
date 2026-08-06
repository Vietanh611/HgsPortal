namespace Hgs.Share.Responses.PermissionDelegation;

public class AssignableRoleResponse
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int OrganizationUnitId { get; set; }
    public string? OrganizationUnitName { get; set; }
}
