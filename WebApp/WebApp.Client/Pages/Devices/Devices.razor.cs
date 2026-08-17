using BlazorBootstrap;
using Hgs.Share.Requests.Devices;
using Hgs.Share.Responses.ApiResponses;
using Hgs.Share.Responses.Devices;
using Hgs.Share.Responses.OrganizationUnits;
using Microsoft.AspNetCore.Components;
using WebApp.Client.Services;
using WebApp.Client.Services.Components;
using WebApp.Client.Services.Network;
using CustomToastService = WebApp.Client.Services.Components.ToastService;

namespace WebApp.Client.Pages.Devices;

public partial class Devices
{
    [Inject] private ApiClient ApiClient { get; set; } = default!;
    [Inject] private DevicesService DevicesService { get; set; } = default!;
    [Inject] private CustomToastService ToastService { get; set; } = default!;
    [Inject] private DialogService DialogService { get; set; } = default!;

    private IEnumerable<DeviceGetAllResponse>? devices;
    private IEnumerable<OrganizationUnitsGetAllResponse>? organizationUnits;
    private string filterStatus = string.Empty;
    private int? filterOrgUnitId;
    private bool isLoading = true;
    private bool isCreating = false;
    private string? createErrorMessage;
    private string? errorMessage;

    private DeviceCreateModal createModal = default!;
    private DeviceSecretModal secretModal = default!;

    protected override async Task OnInitializedAsync()
    {
        await Task.WhenAll(LoadDevices(), LoadOrganizationUnits());
    }

    private async Task LoadOrganizationUnits()
    {
        try
        {
            var response = await ApiClient.GetAsync<ApiResponse<IEnumerable<OrganizationUnitsGetAllResponse>>>("api/organizationunits", silent: true);
            if (response != null && response.Success && response.Data != null)
            {
                organizationUnits = response.Data;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading organization units: {ex.Message}");
        }
    }

    private async Task LoadDevices()
    {
        isLoading = true;
        errorMessage = null;
        try
        {
            var result = await DevicesService.GetDevicesAsync(filterStatus, filterOrgUnitId);
            if (result != null)
            {
                devices = result;
            }
            else
            {
                devices = null;
                errorMessage = !string.IsNullOrWhiteSpace(ApiClient.LastError)
                    ? ApiClient.LastError
                    : "Không thể tải danh sách thiết bị.";
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading devices: {ex.Message}");
            devices = null;
            errorMessage = "Không thể tải danh sách thiết bị. Vui lòng thử lại.";
        }
        finally
        {
            isLoading = false;
        }
    }

    private async Task ApplyFilters() => await LoadDevices();

    private async Task ResetFilters()
    {
        filterStatus = string.Empty;
        filterOrgUnitId = null;
        await LoadDevices();
    }

    private async Task ShowCreateModal()
    {
        await createModal.ShowAsync();
    }

    private async Task CloseCreateModal()
    {
        await createModal.HideAsync();
    }

    private async Task HandleDeviceCreated()
    {
        if (createModal == null)
        {
            return;
        }

        var request = createModal.GetFormModel();
        if (string.IsNullOrWhiteSpace(request.DeviceName))
        {
            createErrorMessage = "Vui lòng nhập tên thiết bị.";
            return;
        }

        isCreating = true;
        createErrorMessage = null;

        try
        {
            var response = await DevicesService.CreatePairingCodeAsync(request);

            if (response != null)
            {
                await createModal.HideAsync();
                await LoadDevices();
                await secretModal.ShowAsync(response);
            }
            else
            {
                createErrorMessage = !string.IsNullOrWhiteSpace(ApiClient.LastError)
                    ? ApiClient.LastError
                    : "Không thể tạo mã ghép. Vui lòng thử lại.";
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error creating pairing code: {ex.Message}");
            createErrorMessage = "Không thể tạo mã ghép. Vui lòng thử lại.";
        }
        finally
        {
            isCreating = false;
        }
    }

    private async Task RegeneratePairingCode(DeviceGetAllResponse device)
    {
        var confirmed = await DialogService.ShowConfirmAsync(
            "Tạo lại mã ghép",
            $"Bạn có chắc muốn tạo mã ghép mới cho thiết bị \"{device.DeviceName}\"?",
            "Mã ghép cũ sẽ hết hiệu lực ngay lập tức.");

        if (!confirmed)
        {
            return;
        }

        var result = await DevicesService.RegeneratePairingCodeAsync(device.Id);
        if (result != null)
        {
            ToastService.ShowSuccess("Đã tạo mã ghép mới.");
            await LoadDevices();
            await secretModal.ShowAsync(result);
        }
        else
        {
            ToastService.ShowError(!string.IsNullOrWhiteSpace(ApiClient.LastError)
                ? ApiClient.LastError
                : "Không thể tạo mã ghép.");
        }
    }

    private async Task ShowDetail(DeviceGetAllResponse device)
    {
        await secretModal.ShowAsync(device);
    }

    private async Task ToggleStatus(DeviceGetAllResponse device)
    {
        var newState = !device.IsEnabled;
        var result = await DevicesService.UpdateStatusAsync(device.Id, newState);
        if (result != null)
        {
            ToastService.ShowSuccess(newState ? "Đã bật thiết bị." : "Đã tắt thiết bị.");
            await LoadDevices();
        }
        else
        {
            ToastService.ShowError(!string.IsNullOrWhiteSpace(ApiClient.LastError)
                ? ApiClient.LastError
                : "Không thể cập nhật trạng thái thiết bị.");
        }
    }

    private async Task RevokeDevice(DeviceGetAllResponse device)
    {
        var confirmed = await DialogService.ShowConfirmAsync(
            "Thu hồi thiết bị",
            $"Bạn có chắc muốn thu hồi thiết bị \"{device.DeviceName}\"?",
            "Thiết bị sẽ bị vô hiệu hóa và mọi phiên đăng nhập liên quan sẽ bị thu hồi. Không thể tự bật lại.");

        if (!confirmed)
        {
            return;
        }

        var result = await DevicesService.RevokeDeviceAsync(device.Id);
        if (result != null)
        {
            ToastService.ShowSuccess("Đã thu hồi thiết bị.");
            await LoadDevices();
        }
        else
        {
            ToastService.ShowError(!string.IsNullOrWhiteSpace(ApiClient.LastError)
                ? ApiClient.LastError
                : "Không thể thu hồi thiết bị.");
        }
    }

    private async Task DeleteDevice(int id, string deviceName)
    {
        var confirmed = await DialogService.ShowConfirmAsync(
            "Xóa thiết bị",
            $"Bạn có chắc muốn xóa thiết bị \"{deviceName}\"?",
            "Thiết bị sẽ bị xóa vĩnh viễn khỏi danh sách.");

        if (!confirmed)
        {
            return;
        }

        var success = await DevicesService.DeleteDeviceAsync(id);
        if (success)
        {
            ToastService.ShowSuccess("Đã xóa thiết bị.");
            await LoadDevices();
        }
        else
        {
            ToastService.ShowError(!string.IsNullOrWhiteSpace(ApiClient.LastError)
                ? ApiClient.LastError
                : "Không thể xóa thiết bị.");
        }
    }

    private string GetOrgUnitName(int? organizationUnitId)
    {
        if (!organizationUnitId.HasValue || organizationUnits == null)
        {
            return "—";
        }

        return organizationUnits.FirstOrDefault(o => o.Id == organizationUnitId.Value)?.Name ?? "—";
    }

    private string GetStatusLabel(string status) => status switch
    {
        "ONLINE" => "Đang hoạt động",
        "ACTIVE" => "Kích hoạt",
        "PENDING" => "Chờ ghép cặp",
        "DISABLED" => "Đã tắt",
        "REVOKED" => "Đã thu hồi",
        _ => status
    };

    private BadgeColor GetStatusColor(string status) => status switch
    {
        "ONLINE" => BadgeColor.Success,
        "ACTIVE" => BadgeColor.Info,
        "PENDING" => BadgeColor.Warning,
        "DISABLED" => BadgeColor.Danger,
        "REVOKED" => BadgeColor.Dark,
        _ => BadgeColor.Secondary
    };
}