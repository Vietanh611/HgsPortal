using Hgs.Share.Attributes;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Entities.Identity
{
    public class Users
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Username { get; set; } = string.Empty;

        [Required]
        [StringLength(200)]
        public string Email { get; set; } = string.Empty;

        [Required]
        [StringLength(500)]
        [AuditIgnore]
        public string PasswordHash { get; set; } = string.Empty;

        [Required]
        [StringLength(200)]
        public string FullName { get; set; } = string.Empty;

        [StringLength(20)]
        public string? PhoneNumber { get; set; }

        [StringLength(500)]
        public string? AvatarUrl { get; set; }
        public int? BravoId { get; set; }
        public int OrganizationUnitId { get; set; }
        public bool IsActive { get; set; } = true;
        public bool IsLocked { get; set; }
        public DateTime? LockoutEnd { get; set; }
        public int FailedLoginCount { get; set; }
        public string? PasswordResetTokenHash { get; set; }
        public DateTime? PasswordResetTokenExpiresAt { get; set; }
        public DateTime? LastLoginAt { get; set; }

        public DateTime CreatedAt { get; set; }
        public int? CreatedBy { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public int? UpdatedBy { get; set; }
        public bool IsDeleted { get; set; }

        #region Navigation Properties
        [AuditIgnore]
        [ForeignKey(nameof(OrganizationUnitId))]
        public virtual OrganizationUnits OrganizationUnit { get; set; } = null!;
        [AuditIgnore]
        public virtual ICollection<UserRoles> UserRoles { get; set; } = new List<UserRoles>();

        #endregion
    }
}
