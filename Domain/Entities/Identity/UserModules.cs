using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Entities.Identity
{
    [Table("UserModules")]
    public class UserModules
    {
        [Key]
        public int Id { get; set; }

        public int? UserId { get; set; }

        public int? ModuleId { get; set; }

        public DateTime CreatedAt { get; set; }

        public int? CreatedBy { get; set; }

        #region Navigation Properties

        [ForeignKey(nameof(UserId))]
        public virtual Users? User { get; set; }

        [ForeignKey(nameof(ModuleId))]
        public virtual Modules? Module { get; set; }

        #endregion
    }
}
