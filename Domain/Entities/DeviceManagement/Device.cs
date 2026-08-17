using Hgs.Share.Attributes;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Domain.Entities.Identity;

namespace Domain.Entities.DeviceManagement
{
    [Table("Devices")]
    public class Device
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(20)]
        public string DeviceType { get; set; } = DeviceTypes.KioskWeb;

        [Required]
        [StringLength(200)]
        public string DeviceName { get; set; } = string.Empty;

        [Required]
        [StringLength(200)]
        public string DeviceIdentifier { get; set; } = string.Empty;

        [StringLength(500)]
        public string? DeviceKeyHash { get; set; }

        [Required]
        [StringLength(20)]
        public string Status { get; set; } = DeviceStatuses.Pending;

        [StringLength(500)]
        public string? PairingCodeHash { get; set; }

        public DateTime? PairingCodeExpiresAt { get; set; }

        public bool IsEnabled { get; set; } = true;

        public DateTime? RevokedAt { get; set; }
        public int? RevokedBy { get; set; }

        public DateTime? LastSeenAt { get; set; }

        [StringLength(45)]
        public string? LastSeenIp { get; set; }

        public int? OrganizationUnitId { get; set; }

        public DateTime CreatedAt { get; set; }
        public int? CreatedBy { get; set; }

        public bool IsDeleted { get; set; }
        public DateTime? DeletedAt { get; set; }
        public int? DeletedBy { get; set; }

        #region Navigation Properties
        [AuditIgnore]
        [ForeignKey(nameof(RevokedBy))]
        public virtual Users? RevokedByUser { get; set; }

        [AuditIgnore]
        [ForeignKey(nameof(DeletedBy))]
        public virtual Users? DeletedByUser { get; set; }

        [AuditIgnore]
        [ForeignKey(nameof(OrganizationUnitId))]
        public virtual OrganizationUnits? OrganizationUnit { get; set; }
        #endregion
    }

    public static class DeviceTypes
    {
        public const string KioskWeb = "KIOSK_WEB";

        public static readonly string[] All = { KioskWeb };

        public static bool IsValid(string? deviceType)
        {
            return !string.IsNullOrWhiteSpace(deviceType) && All.Contains(deviceType);
        }
    }

    public static class DeviceStatuses
    {
        public const string Pending = "PENDING";
        public const string Active = "ACTIVE";
        public const string Revoked = "REVOKED";

        public static readonly string[] All = { Pending, Active, Revoked };

        public static bool IsValid(string? status)
        {
            return !string.IsNullOrWhiteSpace(status) && All.Contains(status);
        }
    }
}