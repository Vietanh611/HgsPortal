namespace Hgs.Share.Requests.OrganizationUnits;

public class OrganizationUnitsUpdateRequest
{
    public string? Code { get; set; }
    public string? Name { get; set; }
    public int? ParentId { get; set; }
    public int? SortOrder { get; set; }
    public bool? IsActive { get; set; }
}
