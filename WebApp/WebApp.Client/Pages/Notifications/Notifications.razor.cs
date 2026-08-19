using BlazorBootstrap;
using Hgs.Share.Responses.Notifications;
using Microsoft.AspNetCore.Components;
using WebApp.Client.Components;
using WebApp.Client.Services.Components;
using WebApp.Client.Services.Data;
using ToastService = WebApp.Client.Services.Components.ToastService;

namespace WebApp.Client.Pages.Notifications;

public partial class Notifications : AuthorizedPageBase
{
    [Inject] private NotificationService NotificationService { get; set; } = default!;
    [Inject] private NavigationManager Navigation { get; set; } = default!;
    [Inject] private ToastService ToastService { get; set; } = default!;

    private Grid<NotificationListItemResponse> grid = default!;
    private string? errorMessage;
    private bool showEmptyState;
    private bool filterExpanded;
    private bool isMarkingAll;

    private string filterCategory = string.Empty;
    private string filterRead = string.Empty;

    private async Task<GridDataProviderResult<NotificationListItemResponse>> NotificationsDataProvider(GridDataProviderRequest<NotificationListItemResponse> request)
    {
        if (!IsInteractive)
        {
            return new GridDataProviderResult<NotificationListItemResponse> { Data = Array.Empty<NotificationListItemResponse>(), TotalCount = 0 };
        }

        errorMessage = null;
        showEmptyState = false;

        try
        {
            var category = string.IsNullOrWhiteSpace(filterCategory) ? null : filterCategory;
            bool? isRead = filterRead switch
            {
                "true" => true,
                "false" => false,
                _ => null
            };

            var result = await NotificationService.GetMyNotificationsAsync(category, isRead, request.PageNumber, request.PageSize);
            if (result != null)
            {
                showEmptyState = result.TotalCount == 0;
                return new GridDataProviderResult<NotificationListItemResponse>
                {
                    Data = result.Items,
                    TotalCount = result.TotalCount
                };
            }

            errorMessage = "Không thể tải thông báo.";
            return new GridDataProviderResult<NotificationListItemResponse>
            {
                Data = new List<NotificationListItemResponse>(),
                TotalCount = 0
            };
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading notifications: {ex.Message}");
            errorMessage = "Không thể tải thông báo. Vui lòng thử lại.";
            return new GridDataProviderResult<NotificationListItemResponse>
            {
                Data = new List<NotificationListItemResponse>(),
                TotalCount = 0
            };
        }
    }

    private async Task ApplyFilters()
    {
        errorMessage = null;
        showEmptyState = false;

        if (grid is null)
        {
            // Grid chưa mount (đang ở empty/error) — mount lại để DataProvider tự chạy
            await InvokeAsync(StateHasChanged);
        }
        else
        {
            await grid.ResetPageNumber();
        }
    }

    private async Task ResetFilters()
    {
        filterCategory = string.Empty;
        filterRead = string.Empty;
        await ApplyFilters();
    }

    private void ToggleFilterCollapse()
    {
        filterExpanded = !filterExpanded;
    }

    private async Task MarkRead(NotificationListItemResponse item)
    {
        var ok = await NotificationService.MarkAsReadAsync(item.Id);
        if (ok)
        {
            await ApplyFilters();
        }
    }

    private async Task MarkAllRead()
    {
        if (isMarkingAll)
        {
            return;
        }

        isMarkingAll = true;
        try
        {
            var ok = await NotificationService.MarkAllAsReadAsync();
            if (ok)
            {
                ToastService.ShowSuccess("Đã đánh dấu tất cả thông báo đã đọc.");
                await ApplyFilters();
            }
            else
            {
                ToastService.ShowError("Không thể đánh dấu tất cả thông báo đã đọc. Vui lòng thử lại.");
            }
        }
        finally
        {
            isMarkingAll = false;
        }
    }

    private void OpenNotification(NotificationListItemResponse item)
    {
        if (!item.IsRead)
        {
            _ = MarkRead(item);
        }
        var url = !string.IsNullOrWhiteSpace(item.ActionUrl) ? item.ActionUrl : "/notifications";
        Navigation.NavigateTo(url);
    }

    internal static string GetCategoryLabel(string category)
    {
        return category switch
        {
            "Security" => "Bảo mật",
            "Permission" => "Phân quyền",
            "System" => "Hệ thống",
            "CustomerSatisfaction" => "Đánh giá khách hàng",
            _ => category
        };
    }

    private static BadgeColor GetCategoryBadgeColor(string category)
    {
        return category switch
        {
            "Security" => BadgeColor.Danger,
            "Permission" => BadgeColor.Warning,
            "System" => BadgeColor.Info,
            "CustomerSatisfaction" => BadgeColor.Primary,
            _ => BadgeColor.Secondary
        };
    }

    private static BadgeColor GetSeverityBadgeColor(string severity)
    {
        return severity switch
        {
            "Critical" => BadgeColor.Danger,
            "High" => BadgeColor.Warning,
            "Warning" => BadgeColor.Warning,
            _ => BadgeColor.Secondary
        };
    }
}