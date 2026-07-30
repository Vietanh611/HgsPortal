namespace Hgs.Share.Responses.UserRoles;

public class UserRolesCreateResponse
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int RoleId { get; set; }
    public DateTime AssignedAt { get; set; }
    public DateTime? ExpiredAt { get; set; }
}
