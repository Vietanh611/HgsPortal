namespace Hgs.Share.Requests.UserRoles;

public class UserRolesAssignMultipleRequest
{
    public int UserId { get; set; }
    public List<int> RoleIds { get; set; } = new();
    public DateTime? ExpiredAt { get; set; }
}
