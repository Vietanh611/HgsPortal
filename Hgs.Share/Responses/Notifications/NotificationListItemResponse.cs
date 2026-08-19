namespace Hgs.Share.Responses.Notifications;

/// <summary>
/// Một dòng thông báo trong danh sách chuông (kèm trạng thái đã đọc của user hiện tại).
/// </summary>
public class NotificationListItemResponse
{
    public long Id { get; set; }

    public string Category { get; set; } = default!;

    public string Severity { get; set; } = "Info";

    public string Title { get; set; } = default!;

    public string? Body { get; set; }

    public string? ActionUrl { get; set; }

    public string? SourceEntityName { get; set; }

    public string? SourceEntityId { get; set; }

    public bool IsRead { get; set; }

    public DateTime CreatedAt { get; set; }
}