namespace Hgs.Share.Requests.PermissionDelegation;

public class RevokeRoleRequest
{
    public int TargetUserId { get; set; }
    public int RoleId { get; set; }
}
