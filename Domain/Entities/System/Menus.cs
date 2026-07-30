using Domain.Entities.Identity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Entities.System
{
    public class Menus
    {
        [Key]
        public int Id { get; set; }
        public int ModuleId { get; set; }
        public int? ParentId { get; set; }
        [Required]
        [MaxLength(100)]
        public string Code { get; set; } = string.Empty;
        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;
        [MaxLength(500)]
        public string? Route { get; set; }
        [MaxLength(200)]
        public string? Component { get; set; }
        [MaxLength(100)]
        public string? Icon { get; set; }
        public int SortOrder { get; set; }
        public bool IsVisible { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public int? CreatedBy { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public int? UpdatedBy { get; set; }

        #region Navigation Properties

        [ForeignKey(nameof(ModuleId))]
        public virtual Modules Module { get; set; } = null!;
        [ForeignKey(nameof(ParentId))]
        public virtual Menus? Parent { get; set; }
        public virtual ICollection<Menus> Children { get; set; } = new List<Menus>();
        [ForeignKey(nameof(CreatedBy))]
        public virtual Users? CreatedByUser { get; set; }
        [ForeignKey(nameof(UpdatedBy))]
        public virtual Users? UpdatedByUser { get; set; }

        #endregion
    }
}
