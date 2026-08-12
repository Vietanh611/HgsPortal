using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Entities.DisplayDevices
{
    [Table("DisplayDevices")]
    public class DisplayDevices
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string DeviceName { get; set; } = string.Empty;

        [Required]
        [StringLength(200)]
        public string DeviceIdentifier { get; set; } = string.Empty;

        [StringLength(20)]
        public string Status { get; set; } = "ACTIVE";
        public bool IsEnabled { get; set; } = true;

        public DateTime? LastSeenAt { get; set; }
    }
}