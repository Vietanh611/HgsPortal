namespace Hgs.Share.Responses.PermissionDelegation;

public class ManageableUserResponse
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public int OrganizationUnitId { get; set; }
    public string? OrganizationUnitName { get; set; }
    public bool IsActive { get; set; }
}
