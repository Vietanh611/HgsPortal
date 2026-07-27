namespace Domain.Entities.System
{
    public class Menus
    {
        public int Id { get; set; }

        public string Code { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;

        public string? Icon { get; set; }

        public string? Url { get; set; }

        public int? ParentId { get; set; }

        public int SortOrder { get; set; }

        public string? PermissionCode { get; set; }

        public bool IsActive { get; set; }

        // Navigation
        public Menus? Parent { get; set; }

        public ICollection<Menus> Children { get; set; } = new List<Menus>();
    }
}
