using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Entities.CustomerSatisfaction
{

    [Table("UnsatisfiedReasons", Schema = "CustomerSatisfaction")]
    public class UnsatisfiedReasons
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(200)]
        public string ReasonName { get; set; } = string.Empty;

        [StringLength(20)]
        public string Status { get; set; } = "ACTIVE";

        public ICollection<EvaluationReasonLinks> EvaluationReasonLinks { get; set; } = new List<EvaluationReasonLinks>();
    }
}
