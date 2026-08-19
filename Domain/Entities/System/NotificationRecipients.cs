using Domain.Entities.Identity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Entities.System;

/// <summary>
/// Trạng thái của một thông báo theo từng người nhận: cùng một sự kiện (<see cref="Notifications"/>)
/// có thể được gửi cho nhiều user, mỗi user đọc/chưa đọc độc lập.
/// </summary>
[Table("NotificationRecipients")]
public class NotificationRecipients
{
    [Key]
    public long Id { get; set; }

    public long NotificationId { get; set; }

    public int UserId { get; set; }

    public bool IsRead { get; set; }

    public DateTime? ReadAt { get; set; }

    #region Navigation

    [ForeignKey(nameof(NotificationId))]
    public Notifications Notification { get; set; } = default!;

    [ForeignKey(nameof(UserId))]
    public Users User { get; set; } = default!;

    #endregion
}