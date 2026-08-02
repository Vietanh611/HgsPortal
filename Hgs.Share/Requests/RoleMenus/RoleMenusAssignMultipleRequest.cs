namespace Hgs.Share.Requests.RoleMenus;

public class RoleMenusAssignMultipleRequest
{
    public int RoleId { get; set; }
    public List<int> MenuIds { get; set; } = new();
}
