using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Entities.CustomerSatisfaction
{
    [Table("Evaluations", Schema = "CustomerSatisfaction")]
    public class Evaluations
    {
        [Key]
        public int Id { get; set; }

        public int? FlightId { get; set; }

        public int? StaffUserId { get; set; }

        public int? DeviceId { get; set; }

        [StringLength(100)]
        public string? CheckinCounterName { get; set; }

        public int RatingLevel { get; set; }

        public int EvaluationType { get; set; }

        public DateTime CreatedAt { get; set; }

        #region Navigation

        [ForeignKey(nameof(DeviceId))]
        public Devices? Device { get; set; }

        public ICollection<EvaluationReasonLinks> EvaluationReasonLinks { get; set; } = new List<EvaluationReasonLinks>();

        #endregion
    }
}
