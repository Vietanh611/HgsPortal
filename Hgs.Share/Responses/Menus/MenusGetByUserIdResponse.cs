namespace Hgs.Share.Responses.Menus;

public class MenusGetByUserIdResponse
{
    public int Id { get; set; }
    public int? ParentId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Route { get; set; }
    public string? Component { get; set; }
    public string? Icon { get; set; }
    public int SortOrder { get; set; }
    public bool IsVisible { get; set; }
    public bool IsActive { get; set; }
    public List<MenusGetByUserIdResponse> Children { get; set; } = new();
}
