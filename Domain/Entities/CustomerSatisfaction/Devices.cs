using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Entities.CustomerSatisfaction
{
    [Table("Devices", Schema = "CustomerSatisfaction")]
    public class Devices
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

        public DateTime? LastSeenAt { get; set; }

        public ICollection<Evaluations> Evaluations { get; set; } = new List<Evaluations>();
    }
}
