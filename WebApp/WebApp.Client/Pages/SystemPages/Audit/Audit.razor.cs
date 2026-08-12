using BlazorBootstrap;
using Hgs.Share.Responses.ApiResponses;
using Hgs.Share.Responses.AuditLogs;
using Microsoft.AspNetCore.Components;
using WebApp.Client.Services.Network;
using AuditDetailModal = WebApp.Client.Pages.SystemPages.Audit.AuditDetailModal;

namespace WebApp.Client.Pages.SystemPages.Audit;

public partial class Audit : ComponentBase
{
    [Inject] private ApiClient ApiClient { get; set; } = default!;
    private AuditDetailModal auditDetailModal = default!;
    private IEnumerable<AuditLogsGetAllResponse>? auditLogs;
    private AuditLogsGetAllResponse? selectedAuditLog;
    private bool isLoading = true;
    private int currentPage = 1;
    private int pageSize = 10;
    private int totalCount = 0;

    protected override async Task OnInitializedAsync()
    {
        await LoadAuditLogs();
    }

    private async Task LoadAuditLogs()
    {
        isLoading = true;
        try
        {
            var response = await ApiClient.GetAsync<ApiResponse<PagedResponse<AuditLogsGetAllResponse>>>($"api/audit?pageNumber={currentPage}&pageSize={pageSize}");
            if (response != null && response.Success && response.Data != null)
            {
                auditLogs = response.Data.Items;
                totalCount = response.Data.TotalCount;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading audit logs: {ex.Message}");
        }
        finally
        {
            isLoading = false;
        }
    }

    private async Task OnPageChanged(int pageNumber)
    {
        currentPage = pageNumber;
        await LoadAuditLogs();
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

    private BadgeColor GetActionBadgeColor(string action)
    {
        return action switch
        {
            "CREATE" => BadgeColor.Success,
            "UPDATE" => BadgeColor.Warning,
            "DELETE" => BadgeColor.Danger,
            _ => BadgeColor.Secondary
        };
    }
}
