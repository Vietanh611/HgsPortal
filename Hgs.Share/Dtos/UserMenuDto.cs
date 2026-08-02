namespace Hgs.Share.Dtos;

public class UserMenuDto
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int MenuId { get; set; }
    public DateTime AssignedAt { get; set; }
    public int? AssignedBy { get; set; }
    public MenuDto Menu { get; set; } = null!;
}
