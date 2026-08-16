using BlazorBootstrap;
using Hgs.Share.Requests.Audit;
using Hgs.Share.Responses.ApiResponses;
using Hgs.Share.Responses.AuditLogs;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System.Globalization;
using System.Net;
using WebApp.Client.Services.Components;
using WebApp.Client.Services.Network;
using CustomToastService = WebApp.Client.Services.Components.ToastService;

namespace WebApp.Client.Pages.SystemPages.Audit;

public partial class Audit : ComponentBase
{
    [Inject] private ApiClient ApiClient { get; set; } = default!;
    [Inject] private CustomToastService ToastService { get; set; } = default!;

    private Grid<AuditLogsGetAllResponse> grid = default!;
    private AuditDetailModal auditDetailModal = default!;
    private AuditLogsGetAllResponse? selectedAuditLog;
    private string? errorMessage;
    private bool showEmptyState;
    private bool filterExpanded;
    private bool isExporting;

    private string? filterKeyword;
    private string? filterEntityName;
    private string filterEntityId = string.Empty;
    private string filterEventCategory = string.Empty;
    private string? filterAction;
    private string filterSuccess = string.Empty;
    private string filterSeverity = string.Empty;
    private DateTime? filterFromDate;
    private DateTime? filterToDate;

    private async Task<GridDataProviderResult<AuditLogsGetAllResponse>> AuditLogsDataProvider(GridDataProviderRequest<AuditLogsGetAllResponse> request)
    {
        errorMessage = null;
        showEmptyState = false;

        try
        {
            var filter = BuildFilter(request.PageNumber, request.PageSize);
            var query = BuildQueryString(filter);
            var response = await ApiClient.GetAsync<ApiResponse<PagedResponse<AuditLogsGetAllResponse>>>($"api/audit?{query}");
            if (response != null && response.Success && response.Data != null)
            {
                showEmptyState = response.Data.TotalCount == 0;
                return new GridDataProviderResult<AuditLogsGetAllResponse>
                {
                    Data = response.Data.Items,
                    TotalCount = response.Data.TotalCount
                };
            }

            errorMessage = !string.IsNullOrWhiteSpace(ApiClient.LastError)
                ? ApiClient.LastError
                : "Không thể tải nhật ký hoạt động.";
            return new GridDataProviderResult<AuditLogsGetAllResponse>
            {
                Data = new List<AuditLogsGetAllResponse>(),
                TotalCount = 0
            };
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading audit logs: {ex.Message}");
            errorMessage = "Không thể tải nhật ký hoạt động. Vui lòng thử lại.";
            return new GridDataProviderResult<AuditLogsGetAllResponse>
            {
                Data = new List<AuditLogsGetAllResponse>(),
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
        filterKeyword = null;
        filterEntityName = null;
        filterEntityId = string.Empty;
        filterEventCategory = string.Empty;
        filterAction = null;
        filterSuccess = string.Empty;
        filterSeverity = string.Empty;
        filterFromDate = null;
        filterToDate = null;
        await ApplyFilters();
    }

    private void ToggleFilterCollapse()
    {
        filterExpanded = !filterExpanded;
    }

    private async Task ExportXlsx()
    {
        if (isExporting)
            return;

        isExporting = true;
        try
        {
            var filter = BuildFilter(1, 20);
            var query = BuildQueryString(filter);
            var fileName = $"audit-log-{DateTime.UtcNow:yyyyMMdd-HHmmss}.xlsx";
            var url = query.Length > 0 ? $"api/audit/export?format=xlsx&{query}" : "api/audit/export?format=xlsx";
            var bytes = await ApiClient.GetFileBytesAsync(url);
            if (bytes != null)
            {
                await JS.InvokeVoidAsync("hgsDownloadFile", fileName, bytes);
                ToastService.ShowSuccess("Đã xuất file nhật ký hoạt động.");
            }
            else
            {
                ToastService.ShowError(ApiClient.LastError ?? "Không thể xuất dữ liệu nhật ký hoạt động.");
            }
        }
        finally
        {
            isExporting = false;
        }
    }

    private AuditLogsFilterRequest BuildFilter(int pageNumber, int pageSize)
    {
        return new AuditLogsFilterRequest
        {
            PageNumber = pageNumber,
            PageSize = pageSize,
            Keyword = string.IsNullOrWhiteSpace(filterKeyword) ? null : filterKeyword.Trim(),
            EntityName = string.IsNullOrWhiteSpace(filterEntityName) ? null : filterEntityName.Trim(),
            EntityId = int.TryParse(filterEntityId, out var entityId) ? entityId : null,
            EventCategory = string.IsNullOrWhiteSpace(filterEventCategory) ? null : filterEventCategory,
            Action = string.IsNullOrWhiteSpace(filterAction) ? null : filterAction.Trim(),
            Success = filterSuccess switch
            {
                "true" => true,
                "false" => false,
                _ => null
            },
            Severity = string.IsNullOrWhiteSpace(filterSeverity) ? null : filterSeverity,
            FromDate = filterFromDate.HasValue ? filterFromDate.Value.Date : null,
            ToDate = filterToDate.HasValue ? filterToDate.Value.Date.AddDays(1).AddTicks(-1) : null
        };
    }

    private static string BuildQueryString(AuditLogsFilterRequest filter)
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
        Add("keyword", filter.Keyword);
        Add("entityName", filter.EntityName);
        Add("entityId", filter.EntityId);
        Add("eventCategory", filter.EventCategory);
        Add("action", filter.Action);
        Add("success", filter.Success);
        Add("severity", filter.Severity);
        Add("fromDate", filter.FromDate?.ToString("o", CultureInfo.InvariantCulture));
        Add("toDate", filter.ToDate?.ToString("o", CultureInfo.InvariantCulture));

        return string.Join("&", parts);
    }

    private async Task ShowDetailModal(AuditLogsGetAllResponse auditLog)
    {
        selectedAuditLog = auditLog;
        await auditDetailModal.ShowAsync();
    }

    private async Task CloseDetailModal()
    {
        await auditDetailModal.HideAsync();
        selectedAuditLog = null;
    }

    private static BadgeColor GetActionBadgeColor(string action)
    {
        return action switch
        {
            "CREATE" => BadgeColor.Success,
            "UPDATE" => BadgeColor.Warning,
            "DELETE" => BadgeColor.Danger,
            "EXPORT" => BadgeColor.Primary,
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

    private static BadgeColor GetEventCategoryBadgeColor(string eventCategory)
    {
        return eventCategory switch
        {
            "Auth" => BadgeColor.Info,
            "Security" => BadgeColor.Danger,
            "Permission" => BadgeColor.Warning,
            _ => BadgeColor.Secondary
        };
    }
}