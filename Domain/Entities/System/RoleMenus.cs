using Domain.Entities.Identity;
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

        public DateTime AssignedAt { get; set; } = DateTime.UtcNow;

        public int? AssignedBy { get; set; }

        #region Navigation Properties

        [ForeignKey(nameof(RoleId))]
        public virtual Roles Role { get; set; } = null!;

        [ForeignKey(nameof(MenuId))]
        public virtual Menus Menu { get; set; } = null!;

        [ForeignKey(nameof(AssignedBy))]
        public virtual Users? AssignedByUser { get; set; }

        #endregion
    }
}
