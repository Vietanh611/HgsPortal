using Hgs.Share.Requests.Devices;
using Hgs.Share.Responses.ApiResponses;
using Hgs.Share.Responses.Devices;
using WebApp.Client.Services.Network;

namespace WebApp.Client.Services.Operation;

/// <summary>
/// Service phía client cho quản lý thiết bị (bao gồm quy trình pairing kiosk qua mã ghép);
/// các method trả null khi lỗi để giao diện hiển thị trạng thái mặc định.
/// </summary>
public class DevicesService
{
    private readonly ApiClient _apiClient;

    public DevicesService(ApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    public async Task<IEnumerable<DeviceGetAllResponse>?> GetDevicesAsync(
        string? status = null,
        int? organizationUnitId = null)
    {
        var query = new List<string>();
        if (!string.IsNullOrWhiteSpace(status))
            query.Add($"status={Uri.EscapeDataString(status)}");
        if (organizationUnitId.HasValue)
            query.Add($"organizationUnitId={organizationUnitId.Value}");

        var url = query.Count > 0 ? $"api/devices?{string.Join("&", query)}" : "api/devices";

        try
        {
            var response = await _apiClient.GetAsync<ApiResponse<IEnumerable<DeviceGetAllResponse>>>(url);

            if (response != null && response.Success && response.Data != null)
            {
                return response.Data;
            }

            return null;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error fetching devices: {ex.Message}");
            return null;
        }
    }

    public async Task<DeviceGetByIdResponse?> GetDeviceByIdAsync(int id)
    {
        try
        {
            var response = await _apiClient.GetAsync<ApiResponse<DeviceGetByIdResponse>>($"api/devices/{id}");

            if (response != null && response.Success && response.Data != null)
            {
                return response.Data;
            }

            return null;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error fetching device: {ex.Message}");
            return null;
        }
    }

    public async Task<DevicePairingCodeCreateResponse?> CreatePairingCodeAsync(DevicePairingCodeCreateRequest request)
    {
        try
        {
            var response = await _apiClient.PostAsync<ApiResponse<DevicePairingCodeCreateResponse>>("api/devices/pairing-code", request);

            if (response != null && response.Success && response.Data != null)
            {
                return response.Data;
            }

            return null;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error creating pairing code: {ex.Message}");
            return null;
        }
    }

    public async Task<DevicePairingCodeRegenerateResponse?> RegeneratePairingCodeAsync(int id)
    {
        try
        {
            var response = await _apiClient.PostAsync<ApiResponse<DevicePairingCodeRegenerateResponse>>($"api/devices/{id}/regenerate-pairing-code");

            if (response != null && response.Success && response.Data != null)
            {
                return response.Data;
            }

            return null;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error regenerating pairing code: {ex.Message}");
            return null;
        }
    }

    public async Task<DevicePairResponse?> PairDeviceAsync(DevicePairRequest request)
    {
        try
        {
            var response = await _apiClient.PostAsync<ApiResponse<DevicePairResponse>>("api/devices/pair", request);

            if (response != null && response.Success && response.Data != null)
            {
                return response.Data;
            }

            return null;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error pairing device: {ex.Message}");
            return null;
        }
    }

    public async Task<DeviceStatusUpdateResponse?> UpdateStatusAsync(int id, bool isEnabled)
    {
        try
        {
            var response = await _apiClient.PatchAsync<ApiResponse<DeviceStatusUpdateResponse>>(
                $"api/devices/{id}/status",
                new DeviceStatusUpdateRequest { IsEnabled = isEnabled });

            if (response != null && response.Success && response.Data != null)
            {
                return response.Data;
            }

            return null;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error updating device status: {ex.Message}");
            return null;
        }
    }

    public async Task<DeviceRevokeResponse?> RevokeDeviceAsync(int id)
    {
        try
        {
            var response = await _apiClient.PostAsync<ApiResponse<DeviceRevokeResponse>>($"api/devices/{id}/revoke");

            if (response != null && response.Success && response.Data != null)
            {
                return response.Data;
            }

            return null;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error revoking device: {ex.Message}");
            return null;
        }
    }

    public async Task<bool> DeleteDeviceAsync(int id)
    {
        try
        {
            return await _apiClient.DeleteAsync($"api/devices/{id}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error deleting device: {ex.Message}");
            return false;
        }
    }
}