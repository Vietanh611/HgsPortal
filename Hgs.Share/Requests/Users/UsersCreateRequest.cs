namespace Hgs.Share.Requests.Users;

public class UsersCreateRequest
{
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public int? BravoId { get; set; }
    public string? PhoneNumber { get; set; }
    public string? AvatarUrl { get; set; }
    public int OrganizationUnitId { get; set; }
    public bool IsActive { get; set; } = true;
}
