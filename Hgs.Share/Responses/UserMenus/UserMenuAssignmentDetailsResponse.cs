namespace Hgs.Share.Responses.UserMenus;

public class UserMenuAssignmentDetailsResponse
{
    public List<int> RoleMenuIds { get; set; } = new();
    public List<int> UserMenuIds { get; set; } = new();
}