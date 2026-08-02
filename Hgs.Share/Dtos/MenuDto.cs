namespace Hgs.Share.Dtos;

public class MenuDto
{
    public int Id { get; set; }
    public int ModuleId { get; set; }
    public int? ParentId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Route { get; set; }
    public string? Component { get; set; }
    public string? Icon { get; set; }
    public int SortOrder { get; set; }
    public bool IsVisible { get; set; }
    public bool IsActive { get; set; }
    public ICollection<MenuDto> Children { get; set; } = new List<MenuDto>();
}
