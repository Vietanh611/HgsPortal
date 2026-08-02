namespace Hgs.Share.Responses.RoleMenus;

public class RoleMenusGetByIdResponse
{
    public int Id { get; set; }
    public int RoleId { get; set; }
    public string RoleCode { get; set; } = string.Empty;
    public string RoleName { get; set; } = string.Empty;
    public int MenuId { get; set; }
    public string MenuCode { get; set; } = string.Empty;
    public string MenuName { get; set; } = string.Empty;
    public DateTime AssignedAt { get; set; }
    public int? AssignedBy { get; set; }
}
