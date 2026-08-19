namespace Hgs.Share.Requests.Notifications;

/// <summary>
/// Bộ lọc danh sách thông báo của user hiện tại. Service tự clamp PageNumber/PageSize
/// (giống AuditLogsFilterRequest) để chống DoS qua [FromQuery].
/// </summary>
public class NotificationFilterRequest
{
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;

    public string? Category { get; set; }

    public bool? IsRead { get; set; }
}