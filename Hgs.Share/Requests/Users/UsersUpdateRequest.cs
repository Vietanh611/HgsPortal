namespace Hgs.Share.Requests.Users;

public class UsersUpdateRequest
{
    public string? Email { get; set; }
    public string? FullName { get; set; }
    public string? PhoneNumber { get; set; }
    public string? AvatarUrl { get; set; }
    public int? OrganizationUnitId { get; set; }
    public bool? IsActive { get; set; }
}
