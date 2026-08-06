using Domain.Entities.Identity;
using Hgs.Share.Attributes;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Entities.System
{
    [Table("RoleMenus")]
    public class RoleMenus
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int RoleId { get; set; }

        [Required]
        public int MenuId { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public int? CreatedBy { get; set; }

        #region Navigation Properties
        [AuditIgnore]
        [ForeignKey(nameof(RoleId))]
        public virtual Roles Role { get; set; } = null!;
        [AuditIgnore]
        [ForeignKey(nameof(MenuId))]
        public virtual Menus Menu { get; set; } = null!;
        [AuditIgnore]
        [ForeignKey(nameof(CreatedBy))]
        public virtual Users? CreatedByUser { get; set; }

        #endregion
    }
}
