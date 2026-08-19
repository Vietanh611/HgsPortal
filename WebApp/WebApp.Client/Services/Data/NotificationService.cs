using Hgs.Share.Responses.ApiResponses;
using Hgs.Share.Responses.Notifications;
using WebApp.Client.Services.Network;

namespace WebApp.Client.Services.Data;

/// <summary>
/// Service phía client cho chuông thông báo + trang lịch sử thông báo. Các method trả
/// null/false khi lỗi để giao diện hiển thị trạng thái mặc định (không ném exception).
/// </summary>
public class NotificationService
{
    private readonly ApiClient _apiClient;

    public NotificationService(ApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    public async Task<PagedResponse<NotificationListItemResponse>?> GetMyNotificationsAsync(
        string? category = null,
        bool? isRead = null,
        int pageNumber = 1,
        int pageSize = 20)
    {
        var query = new List<string> { $"pageNumber={pageNumber}", $"pageSize={pageSize}" };
        if (!string.IsNullOrWhiteSpace(category))
        {
            query.Add($"category={Uri.EscapeDataString(category)}");
        }
        if (isRead.HasValue)
        {
            query.Add($"isRead={isRead.Value.ToString().ToLowerInvariant()}");
        }

        try
        {
            var response = await _apiClient.GetAsync<ApiResponse<PagedResponse<NotificationListItemResponse>>>(
                $"api/notifications?{string.Join("&", query)}",
                silent: true);

            if (response != null && response.Success && response.Data != null)
            {
                return response.Data;
            }

            return null;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error fetching notifications: {ex.Message}");
            return null;
        }
    }

    public async Task<int> GetUnreadCountAsync()
    {
        try
        {
            var response = await _apiClient.GetAsync<ApiResponse<int>>("api/notifications/unread-count", silent: true);
            if (response is { Success: true })
            {
                return response.Data;
            }
            return 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error fetching unread count: {ex.Message}");
            return 0;
        }
    }

    public async Task<bool> MarkAsReadAsync(long notificationId)
    {
        try
        {
            return await _apiClient.PutAsync($"api/notifications/{notificationId}/read", new { });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error marking notification as read: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> MarkAllAsReadAsync()
    {
        try
        {
            return await _apiClient.PutAsync("api/notifications/read-all", new { });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error marking all notifications as read: {ex.Message}");
            return false;
        }
    }
}