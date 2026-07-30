namespace Hgs.Share.Responses.UserRoles;

public class UserRolesGetAllResponse
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string UserFullName { get; set; } = string.Empty;
    public int RoleId { get; set; }
    public string RoleCode { get; set; } = string.Empty;
    public string RoleName { get; set; } = string.Empty;
    public DateTime AssignedAt { get; set; }
    public int? AssignedBy { get; set; }
    public DateTime? ExpiredAt { get; set; }
}
