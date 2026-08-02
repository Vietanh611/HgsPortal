namespace Hgs.Share.Requests.UserMenus;

public class UserMenusAssignMultipleRequest
{
    public int UserId { get; set; }
    public List<int> MenuIds { get; set; } = new();
}
