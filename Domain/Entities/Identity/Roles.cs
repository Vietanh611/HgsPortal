using Hgs.Share.Attributes;

namespace Domain.Entities.Identity
{
    public class Roles
    {
        public int Id { get; set; }

        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }

        public int? OrganizationUnitId { get; set; }
        public string DataScope { get; set; } = "Self";

        public bool IsSystemRole { get; set; }
        public bool IsActive { get; set; }

        public DateTime CreatedAt { get; set; }
        public int? CreatedBy { get; set; }

        public OrganizationUnits? OrganizationUnit { get; set; }

        [AuditIgnore]
        public ICollection<UserRoles> UserRoles { get; set; } = new List<UserRoles>();
        [AuditIgnore]
        public ICollection<System.RoleMenus> RoleMenus { get; set; } = new List<System.RoleMenus>();
    }
}
