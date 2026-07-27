using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Entities.ACDM
{
    [Table("Flight")]
    public class FlightACDM
    {
        [Key]
        public int Id { get; set; }
        public int? FlightId { get; set; }
        public int? LinkFlight { get; set; }
        [StringLength(10)]
        public string? FlightDate { get; set; }
        [StringLength(10)]
        public string? FlightNo { get; set; }
        [StringLength(1)]
        public string? ArrDep { get; set; }
        [StringLength(10)]
        public string? Route { get; set; }
        public DateTime? FlightDateTime { get; set; }
        [StringLength(10)]
        public string? Nature { get; set; }
        [StringLength(500)]
        public string? Remark { get; set; }

        [StringLength(10)]
        public string? Status { get; set; }

        [StringLength(5)]
        public string? Apark { get; set; }

        [StringLength(5)]
        public string? Dpark { get; set; }

        [StringLength(5)]
        public string? Belt { get; set; }

        [StringLength(5)]
        public string? Dgate { get; set; }

        [StringLength(5)]
        public string? SIBT { get; set; }

        [StringLength(5)]
        public string? EIBT { get; set; }

        [StringLength(5)]
        public string? ELDT { get; set; }

        [StringLength(5)]
        public string? ALDT { get; set; }

        [StringLength(5)]
        public string? AIBT { get; set; }

        [StringLength(5)]
        public string? ACGT { get; set; }

        [StringLength(5)]
        public string? ARDT { get; set; }

        [StringLength(5)]
        public string? AEGT { get; set; }

        [StringLength(5)]
        public string? ASBT { get; set; }

        [StringLength(5)]
        public string? CLSD { get; set; }

        [StringLength(5)]
        public string? TSAT { get; set; }

        [StringLength(5)]
        public string? TOBT { get; set; }

        [StringLength(5)]
        public string? AOBT { get; set; }

        public int? ETTT { get; set; }

        [StringLength(20)]
        public string? ACNO { get; set; }

        [StringLength(20)]
        public string? ACTP { get; set; }

        public int? LichbayFlightId { get; set; }

        public int? ViewType { get; set; }

        [StringLength(5)]
        public string? ATOT { get; set; }

        [StringLength(5)]
        public string? SOBT { get; set; }

        [StringLength(5)]
        public string? EOBT { get; set; }

        public DateTime? DateModified { get; set; }

        public string? MVT { get; set; }
    }
}
