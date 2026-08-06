using Hgs.Share.Attributes;

namespace Domain.Entities.Identity
{
    public class OrganizationUnits
    {
        public int Id { get; set; }

        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;

        public int? ParentId { get; set; }
        public string? Path { get; set; }

        public int Level { get; set; }
        public int SortOrder { get; set; }

        public bool IsActive { get; set; }

        public DateTime CreatedAt { get; set; }
        public int? CreatedBy { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public int? UpdatedBy { get; set; }

        [AuditIgnore]
        public OrganizationUnits? Parent { get; set; }
        [AuditIgnore]
        public ICollection<OrganizationUnits> Children { get; set; } = new List<OrganizationUnits>();
        [AuditIgnore]
        public ICollection<Users> Users { get; set; } = new List<Users>();
        [AuditIgnore]
        public ICollection<Roles> Roles { get; set; } = new List<Roles>();
    }
}
