namespace Hgs.Share.Responses.PermissionDelegation;

public class UserEffectivePermissionsResponse
{
    public int UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public List<RoleInfo> Roles { get; set; } = new();
    public List<MenuInfo> Menus { get; set; } = new();
}

public class RoleInfo
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
}

public class MenuInfo
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
}
