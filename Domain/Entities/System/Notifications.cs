using Domain.Entities.Identity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Entities.System;

/// <summary>
/// Một bản ghi = một sự kiện nghiệp vụ/bảo mật cần thông báo cho ít nhất một user.
/// Nội dung thông báo được tách khỏi trạng thái đã đọc (bảng <c>NotificationRecipients</c>)
/// vì một sự kiện có thể được gửi cho nhiều người, mỗi người đọc độc lập.
/// </summary>
[Table("Notifications")]
public class Notifications
{
    [Key]
    public long Id { get; set; }

    /// <summary>
    /// Nhóm người nhận quan tâm — quyền nhận broadcast của category được phân theo menu
    /// RBAC (xem <c>Core.Constants.NotificationCategories</c>); category chỉ là nhãn, không
    /// còn bộ lọc preference riêng của user.
    /// </summary>
    [Required]
    [StringLength(30)]
    public string Category { get; set; } = default!;

    /// <summary>Info | Warning | Critical — đồng bộ khái niệm mức độ với audit log.</summary>
    [Required]
    [StringLength(20)]
    public string Severity { get; set; } = "Info";

    /// <summary>Tiêu đề ngắn hiển thị trong dropdown chuông thông báo.</summary>
    [Required]
    [StringLength(200)]
    public string Title { get; set; } = default!;

    [StringLength(1000)]
    public string? Body { get; set; }

    /// <summary>Route Blazor điều hướng khi user click thông báo (VD: "/users").</summary>
    [StringLength(300)]
    public string? ActionUrl { get; set; }

    /// <summary>Tên entity gốc (VD: "Users", "CustomerSatisfaction.Evaluations") — dùng để gộp/tránh trùng cho sự kiện theo thời gian.</summary>
    [StringLength(100)]
    public string? SourceEntityName { get; set; }

    [StringLength(50)]
    public string? SourceEntityId { get; set; }

    /// <summary>User/hệ thống gây ra sự kiện (null nếu sự kiện từ background job).</summary>
    public int? TriggeredByUserId { get; set; }

    /// <summary>Khớp với <c>AuditLogs.CorrelationId</c> nếu sự kiện có audit log tương ứng.</summary>
    [StringLength(50)]
    public string? CorrelationId { get; set; }

    public DateTime CreatedAt { get; set; }

    /// <summary>Mốc dọn dẹp tự động (mặc định CreatedAt + 30 ngày) — sau mốc này bản ghi bị xóa cứng bởi job retention.</summary>
    public DateTime? ExpiresAt { get; set; }

    #region Navigation

    [ForeignKey(nameof(TriggeredByUserId))]
    public Users? TriggeredByUser { get; set; }

    public ICollection<NotificationRecipients> Recipients { get; set; } = new List<NotificationRecipients>();

    #endregion
}