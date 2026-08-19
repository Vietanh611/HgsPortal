using BlazorBootstrap;
using Hgs.Share.Responses.Notifications;
using Microsoft.AspNetCore.Components;
using WebApp.Client.Services.Data;
using WebApp.Client.Services.Notification;

namespace WebApp.Client.Components.Notifications;

public partial class NotificationBell : ComponentBase, IAsyncDisposable
{
    [Inject] private NotificationService NotificationService { get; set; } = default!;
    [Inject] private NotificationPollingService Polling { get; set; } = default!;
    [Inject] private NavigationManager Navigation { get; set; } = default!;

    private Dropdown dropdown = default!;
    private List<NotificationListItemResponse> Notifications { get; set; } = new();
    private int UnreadCount { get; set; }
    private bool IsLoading { get; set; } = true;

    protected override void OnInitialized()
    {
        // Đọc trạng thái hiện tại nếu service đã poll xong trước khi component mount
        // (MainLayout khởi động poll ngay khi xác thực), tránh phải đợi sự kiện đầu tiên.
        UnreadCount = Polling.UnreadCount;
        Notifications = Polling.Notifications;
        if (Polling.UnreadCount > 0 || Polling.Notifications.Any())
        {
            IsLoading = false;
        }

        Polling.StateChanged += OnPollStateChanged;
    }

    /// <summary>
    /// Chuông KHÔNG tự gọi API/poll như phiên bản cũ (nguyên nhân race 2 request song song
    /// cùng refresh token → reuse detection → bị đá ra login). Dữ liệu đến từ
    /// <see cref="NotificationPollingService"/>, được MainLayout khởi động/dừng theo phiên.
    /// </summary>
    private void OnPollStateChanged()
    {
        _ = InvokeAsync(() =>
        {
            UnreadCount = Polling.UnreadCount;
            Notifications = Polling.Notifications;
            IsLoading = false;
            StateHasChanged();
        });
    }

    private async Task OpenNotification(NotificationListItemResponse item)
    {
        if (!item.IsRead)
        {
            await NotificationService.MarkAsReadAsync(item.Id);
            UnreadCount = Math.Max(0, UnreadCount - 1);
            item.IsRead = true;
        }
        await dropdown.HideAsync();
        var url = !string.IsNullOrWhiteSpace(item.ActionUrl) ? item.ActionUrl : "/notifications";
        Navigation.NavigateTo(url);
    }

    private async Task MarkAllRead()
    {
        if (UnreadCount == 0)
        {
            return;
        }
        var ok = await NotificationService.MarkAllAsReadAsync();
        if (ok)
        {
            UnreadCount = 0;
            foreach (var n in Notifications)
            {
                n.IsRead = true;
            }
        }
    }

    private static string FormatTime(DateTime createdAt)
    {
        var local = createdAt.ToLocalTime();
        var diff = DateTime.Now - local;
        if (diff.TotalMinutes < 1)
        {
            return "Vừa xong";
        }
        if (diff.TotalMinutes < 60)
        {
            return $"{diff.TotalMinutes:0} phút trước";
        }
        if (diff.TotalHours < 24)
        {
            return $"{diff.TotalHours:0} giờ trước";
        }
        return local.ToString("dd/MM/yyyy HH:mm");
    }

    public ValueTask DisposeAsync()
    {
        Polling.StateChanged -= OnPollStateChanged;
        return ValueTask.CompletedTask;
    }
}