using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Entities.Identity
{
    public class UserRoles
    {
        [Key]
        public int Id { get; set; }

        public int UserId { get; set; }

        public int RoleId { get; set; }

        public DateTime AssignedAt { get; set; }

        public int? AssignedBy { get; set; }

        public DateTime? ExpiredAt { get; set; }

        #region Navigation

        [ForeignKey(nameof(UserId))]
        public virtual Users User { get; set; } = null!;

        [ForeignKey(nameof(RoleId))]
        public virtual Roles Role { get; set; } = null!;

        #endregion
    }
}
