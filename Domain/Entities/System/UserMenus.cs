using Domain.Entities.Identity;
using Hgs.Share.Attributes;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Entities.System
{
    [Table("UserMenus")]
    public class UserMenus
    {
        [Key]
        public int Id { get; set; }
        [Required]
        public int UserId { get; set; }

        [Required]
        public int MenuId { get; set; }

        public DateTime AssignedAt { get; set; } = DateTime.UtcNow;

        public int? AssignedBy { get; set; }

        #region Navigation Properties
        [AuditIgnore]
        [ForeignKey(nameof(UserId))]
        public virtual Users User { get; set; } = null!;
        [AuditIgnore]
        [ForeignKey(nameof(MenuId))]
        public virtual Menus Menu { get; set; } = null!;
        [AuditIgnore]
        [ForeignKey(nameof(AssignedBy))]
        public virtual Users? AssignedByUser { get; set; }

        #endregion
    }
}
