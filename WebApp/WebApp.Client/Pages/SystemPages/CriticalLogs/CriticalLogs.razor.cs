using BlazorBootstrap;
using Hgs.Share.Requests.CriticalLogs;
using Hgs.Share.Responses.ApiResponses;
using Hgs.Share.Responses.CriticalLogs;
using Microsoft.AspNetCore.Components;
using System.Globalization;
using System.Net;
using WebApp.Client.Components;
using WebApp.Client.Services.Network;

namespace WebApp.Client.Pages.SystemPages.CriticalLogs;

public partial class CriticalLogs : AuthorizedPageBase
{
    [Inject] private ApiClient ApiClient { get; set; } = default!;

    private Grid<CriticalLogsGetAllResponse> grid = default!;
    private CriticalLogDetailModal detailModal = default!;
    private CriticalLogsGetAllResponse? selectedLog;
    private string? errorMessage;
    private bool showEmptyState;
    private bool filterExpanded;

    private string filterLevel = string.Empty;
    private string? filterKeyword;
    private DateTime? filterFromDate;
    private DateTime? filterToDate;

    private async Task<GridDataProviderResult<CriticalLogsGetAllResponse>> CriticalLogsDataProvider(GridDataProviderRequest<CriticalLogsGetAllResponse> request)
    {
        if (!IsInteractive)
        {
            return new GridDataProviderResult<CriticalLogsGetAllResponse> { Data = Array.Empty<CriticalLogsGetAllResponse>(), TotalCount = 0 };
        }

        errorMessage = null;
        showEmptyState = false;

        try
        {
            var filter = BuildFilter(request.PageNumber, request.PageSize);
            var query = BuildQueryString(filter);
            var response = await ApiClient.GetAsync<ApiResponse<PagedResponse<CriticalLogsGetAllResponse>>>($"api/criticallogs?{query}");
            if (response != null && response.Success && response.Data != null)
            {
                showEmptyState = response.Data.TotalCount == 0;
                return new GridDataProviderResult<CriticalLogsGetAllResponse>
                {
                    Data = response.Data.Items,
                    TotalCount = response.Data.TotalCount
                };
            }

            errorMessage = !string.IsNullOrWhiteSpace(ApiClient.LastError)
                ? ApiClient.LastError
                : "Không thể tải nhật ký lỗi.";
            return new GridDataProviderResult<CriticalLogsGetAllResponse>
            {
                Data = new List<CriticalLogsGetAllResponse>(),
                TotalCount = 0
            };
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading critical logs: {ex.Message}");
            errorMessage = "Không thể tải nhật ký lỗi. Vui lòng thử lại.";
            return new GridDataProviderResult<CriticalLogsGetAllResponse>
            {
                Data = new List<CriticalLogsGetAllResponse>(),
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
        filterLevel = string.Empty;
        filterKeyword = null;
        filterFromDate = null;
        filterToDate = null;
        await ApplyFilters();
    }

    private void ToggleFilterCollapse()
    {
        filterExpanded = !filterExpanded;
    }

    private CriticalLogsFilterRequest BuildFilter(int pageNumber, int pageSize)
    {
        return new CriticalLogsFilterRequest
        {
            PageNumber = pageNumber,
            PageSize = pageSize,
            Level = string.IsNullOrWhiteSpace(filterLevel) ? null : filterLevel,
            Keyword = string.IsNullOrWhiteSpace(filterKeyword) ? null : filterKeyword.Trim(),
            FromDate = filterFromDate.HasValue ? filterFromDate.Value.Date : null,
            ToDate = filterToDate.HasValue ? filterToDate.Value.Date.AddDays(1).AddTicks(-1) : null
        };
    }

    private static string BuildQueryString(CriticalLogsFilterRequest filter)
    {
        var parts = new List<string>();
        void Add(string key, object? value)
        {
            if (value is null)
                return;
            parts.Add($"{key}={WebUtility.UrlEncode(value.ToString())}");
        }

        Add("pageNumber", filter.PageNumber);
        Add("pageSize", filter.PageSize);
        Add("level", filter.Level);
        Add("keyword", filter.Keyword);
        Add("fromDate", filter.FromDate?.ToString("o", CultureInfo.InvariantCulture));
        Add("toDate", filter.ToDate?.ToString("o", CultureInfo.InvariantCulture));

        return string.Join("&", parts);
    }

    private async Task ShowDetailModal(CriticalLogsGetAllResponse log)
    {
        selectedLog = log;
        await detailModal.ShowAsync();
    }

    private async Task CloseDetailModal()
    {
        await detailModal.HideAsync();
        selectedLog = null;
    }

    private static BadgeColor GetLevelBadgeColor(string? level)
    {
        return level switch
        {
            "Fatal" => BadgeColor.Danger,
            "Error" => BadgeColor.Warning,
            _ => BadgeColor.Secondary
        };
    }

    private static string Truncate(string? value, int maxLength)
    {
        if (string.IsNullOrEmpty(value))
            return "N/A";
        return value.Length <= maxLength ? value : value[..maxLength] + "...";
    }
}
