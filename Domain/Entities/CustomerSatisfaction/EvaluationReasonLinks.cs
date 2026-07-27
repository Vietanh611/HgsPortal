using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Entities.CustomerSatisfaction
{
    [Table("EvaluationReasonLinks", Schema = "CustomerSatisfaction")]
    public class EvaluationReasonLinks
    {
        public int EvaluationId { get; set; }

        public int ReasonId { get; set; }

        [ForeignKey(nameof(EvaluationId))]
        public Evaluations? Evaluation { get; set; }

        [ForeignKey(nameof(ReasonId))]
        public UnsatisfiedReasons? Reason { get; set; }
    }
}
