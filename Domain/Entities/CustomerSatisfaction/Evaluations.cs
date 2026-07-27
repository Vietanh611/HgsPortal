using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Entities.CustomerSatisfaction
{
    [Table("Evaluations", Schema = "CustomerSatisfaction")]
    public class Evaluations
    {
        [Key]
        public int Id { get; set; }

        public int FlightId { get; set; }

        public int DeviceId { get; set; }

        public int Rating { get; set; }

        [StringLength(1000)]
        public string? Comment { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        #region Navigation

        [ForeignKey(nameof(DeviceId))]
        public Devices? Device { get; set; }

        public ICollection<EvaluationReasonLinks> EvaluationReasonLinks { get; set; } = new List<EvaluationReasonLinks>();

        #endregion
    }
}
