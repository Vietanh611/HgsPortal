namespace Hgs.Share.Requests.UserRoles;

public class UserRolesCreateRequest
{
    public int UserId { get; set; }
    public int RoleId { get; set; }
    public DateTime? ExpiredAt { get; set; }
}
