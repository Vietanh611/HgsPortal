using System.ComponentModel.DataAnnotations;

namespace Hgs.Share.Requests.Notifications;

/// <summary>
/// Yêu cầu tạo một thông báo cho nhóm người nhận. Đây là DTO nội bộ service layer
/// (không bind từ HTTP) — nơi gọi (các service nghiệp vụ) khai báo đủ nội dung sự kiện.
/// </summary>
public class NotifyRequest
{
    /// <summary>Phải thuộc danh sách hằng số trong <c>Core.Constants.NotificationCategories</c>.</summary>
    [Required(ErrorMessage = "Vui lòng nhập category.")]
    [StringLength(30, ErrorMessage = "Category tối đa 30 ký tự.")]
    public string Category { get; set; } = default!;

    /// <summary>Info | Warning | Critical — đồng bộ với mức độ audit log.</summary>
    [StringLength(20, ErrorMessage = "Severity tối đa 20 ký tự.")]
    public string Severity { get; set; } = "Info";

    [Required(ErrorMessage = "Vui lòng nhập tiêu đề.")]
    [StringLength(200, ErrorMessage = "Tiêu đề tối đa 200 ký tự.")]
    public string Title { get; set; } = default!;

    [StringLength(1000, ErrorMessage = "Nội dung tối đa 1000 ký tự.")]
    public string? Body { get; set; }

    [StringLength(300, ErrorMessage = "ActionUrl tối đa 300 ký tự.")]
    public string? ActionUrl { get; set; }

    [StringLength(100, ErrorMessage = "SourceEntityName tối đa 100 ký tự.")]
    public string? SourceEntityName { get; set; }

    [StringLength(50, ErrorMessage = "SourceEntityId tối đa 50 ký tự.")]
    public string? SourceEntityId { get; set; }

    public int? TriggeredByUserId { get; set; }

    [StringLength(50, ErrorMessage = "CorrelationId tối đa 50 ký tự.")]
    public string? CorrelationId { get; set; }
}