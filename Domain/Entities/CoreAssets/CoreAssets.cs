using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Entities.CoreAssets
{
    [Table("Core_Assets")]
    public class CoreAssets
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Code { get; set; } = string.Empty;

        [StringLength(255)]
        public string? FileName { get; set; }

        [StringLength(100)]
        public string? ContentType { get; set; }

        [Required]
        [StringLength(1000)]
        public string StoragePath { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string AssetType { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;
    }
}