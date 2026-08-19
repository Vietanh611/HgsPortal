using Core.Interfaces.Notifications;
using Hgs.Share.Requests.Notifications;
using Hgs.Share.Responses.ApiResponses;
using Hgs.Share.Responses.Notifications;
using API.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ApiController]
[Route("api/notifications")]
[Authorize]
public class NotificationsController : ControllerBase
{
    private readonly INotificationService _notificationService;

    public NotificationsController(INotificationService notificationService)
    {
        _notificationService = notificationService;
    }

    /// <summary>
    /// Danh sách thông báo của user đang đăng nhập (phân trang, lọc category/trạng thái đã đọc).
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResponse<NotificationListItemResponse>>>> GetMyNotifications(
        [FromQuery] NotificationFilterRequest filter,
        CancellationToken cancellationToken)
    {
        if (!User.TryGetCurrentUserId(out var userId))
        {
            return Unauthorized(ApiResponse.FailResponse("User not authenticated", 401));
        }

        var result = await _notificationService.GetMyNotificationsAsync(userId, filter, cancellationToken);
        return Ok(ApiResponse<PagedResponse<NotificationListItemResponse>>.SuccessResponse(result, "Lấy danh sách thông báo thành công", 200));
    }

    /// <summary>Số thông báo chưa đọc — dùng cho badge chuông thông báo (polling 60s).</summary>
    [HttpGet("unread-count")]
    public async Task<ActionResult<ApiResponse<int>>> GetUnreadCount(CancellationToken cancellationToken)
    {
        if (!User.TryGetCurrentUserId(out var userId))
        {
            return Unauthorized(ApiResponse.FailResponse("User not authenticated", 401));
        }

        var count = await _notificationService.GetUnreadCountAsync(userId, cancellationToken);
        return Ok(ApiResponse<int>.SuccessResponse(count, "Lấy số thông báo chưa đọc thành công", 200));
    }

    /// <summary>
    /// Đánh dấu một thông báo đã đọc. UserId lấy từ JWT (không từ param/body) — chống IDOR.
    /// </summary>
    [HttpPut("{notificationId:long}/read")]
    public async Task<ActionResult<ApiResponse>> MarkAsRead(long notificationId, CancellationToken cancellationToken)
    {
        if (!User.TryGetCurrentUserId(out var userId))
        {
            return Unauthorized(ApiResponse.FailResponse("User not authenticated", 401));
        }

        await _notificationService.MarkAsReadAsync(userId, notificationId, cancellationToken);
        return Ok(ApiResponse.SuccessResponse("Đã đánh dấu đã đọc", 200));
    }

    /// <summary>Đánh dấu toàn bộ thông báo của user đang đăng nhập đã đọc.</summary>
    [HttpPut("read-all")]
    public async Task<ActionResult<ApiResponse>> MarkAllAsRead(CancellationToken cancellationToken)
    {
        if (!User.TryGetCurrentUserId(out var userId))
        {
            return Unauthorized(ApiResponse.FailResponse("User not authenticated", 401));
        }

        await _notificationService.MarkAllAsReadAsync(userId, cancellationToken);
        return Ok(ApiResponse.SuccessResponse("Đã đánh dấu tất cả đã đọc", 200));
    }
}