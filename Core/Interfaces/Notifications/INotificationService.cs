using Hgs.Share.Requests.Notifications;
using Hgs.Share.Responses.ApiResponses;
using Hgs.Share.Responses.Notifications;

namespace Core.Interfaces.Notifications;

public interface INotificationService
{
    /// <summary>
    /// Tạo thông báo cho danh sách userId cụ thể. Nơi gọi PHẢI bọc try/catch: lỗi gửi thông báo
    /// không được làm hỏng nghiệp vụ chính (VD: "gán role thành công nhưng notify lỗi" không được
    /// rollback nghiệp vụ gán role).
    /// </summary>
    Task NotifyUsersAsync(NotifyRequest request, IEnumerable<int> userIds, CancellationToken cancellationToken = default);

    /// <summary>Broadcast theo quyền: gửi cho toàn bộ user nắm menu code (gồm SUPER_ADMIN) — người nhận được tra tự động, nơi gọi không phải tự truy vấn.</summary>
    Task NotifyByMenuPermissionAsync(NotifyRequest request, string menuCode, CancellationToken cancellationToken = default);

    /// <summary>
    /// Broadcast theo category: tự resolve menu (quyền) tối thiểu của category từ
    /// <see cref="Core.Constants.NotificationCategories"/> rồi gọi
    /// <see cref="NotifyByMenuPermissionAsync"/>. Category không có ánh xạ menu sẽ ném
    /// <see cref="KeyNotFoundException"/> — category đó không nên broadcast.
    /// </summary>
    /// <param name="orgUnitId">
    /// Org unit phát sinh sự kiện. Khi có giá trị, chỉ user sở hữu menu (hoặc SUPER_ADMIN)
    /// có org bằng hoặc là tổ tiên của org này theo <c>Path</c> mới nhận; org không tồn tại
    /// thì fallback về toàn bộ user có menu. <see langword="null"/> = không lọc theo org.
    /// </param>
    Task NotifyByCategoryAsync(NotifyRequest request, int? orgUnitId = null, CancellationToken cancellationToken = default);

    /// <summary>Tạo thông báo cho toàn bộ user giữ role SUPER_ADMIN hoạt động.</summary>
    Task NotifySuperAdminsAsync(NotifyRequest request, CancellationToken cancellationToken = default);

    /// <summary>Danh sách thông báo của user hiện tại, phân trang + lọc theo category/trạng thái đã đọc.</summary>
    Task<PagedResponse<NotificationListItemResponse>> GetMyNotificationsAsync(int userId, NotificationFilterRequest filter, CancellationToken cancellationToken = default);

    /// <summary>Số thông báo chưa đọc của user — dùng cho badge chuông.</summary>
    Task<int> GetUnreadCountAsync(int userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Đánh dấu đã đọc một thông báo. UserId lấy từ ClaimsPrincipal, không lấy từ param/body —
    /// query luôn điều kiện theo UserId để chặn IDOR.
    /// </summary>
    Task MarkAsReadAsync(int userId, long notificationId, CancellationToken cancellationToken = default);

    Task MarkAllAsReadAsync(int userId, CancellationToken cancellationToken = default);
}