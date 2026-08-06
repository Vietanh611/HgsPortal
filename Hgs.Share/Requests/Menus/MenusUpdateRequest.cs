namespace Hgs.Share.Requests.Menus;

public class MenusUpdateRequest
{

    public int? ParentId { get; set; }

    public string? Code { get; set; }

    public string? Name { get; set; }

    public string? Route { get; set; }

    public string? Component { get; set; }

    public string? Icon { get; set; }

    public int? SortOrder { get; set; }

    public bool? IsVisible { get; set; }

    public bool? IsActive { get; set; }
}
