using Hgs.Share.Attributes;
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

        #region Navigation
        [AuditIgnore]
        [ForeignKey(nameof(UserId))]
        public virtual Users User { get; set; } = null!;
        [AuditIgnore]
        [ForeignKey(nameof(RoleId))]
        public virtual Roles Role { get; set; } = null!;

        #endregion
    }
}
