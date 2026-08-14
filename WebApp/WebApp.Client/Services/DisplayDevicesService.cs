using Hgs.Share.Requests.DisplayDevices;
using Hgs.Share.Responses.ApiResponses;
using Hgs.Share.Responses.DisplayDevices;
using Microsoft.AspNetCore.Components;
using WebApp.Client.Services.Network;

namespace WebApp.Client.Services;

public class DisplayDevicesService
{
    private readonly ApiClient _apiClient;
    private readonly NavigationManager _navigationManager;

    public DisplayDevicesService(ApiClient apiClient, NavigationManager navigationManager)
    {
        _apiClient = apiClient;
        _navigationManager = navigationManager;
    }

    public async Task<IEnumerable<DisplayDevicesGetAllResponse>?> GetDisplayDevicesAsync()
    {
        try
        {
            var response = await _apiClient.GetAsync<ApiResponse<IEnumerable<DisplayDevicesGetAllResponse>>>("api/displaydevices");
            
            if (response != null && response.Success && response.Data != null)
            {
                return response.Data;
            }
            
            return null;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error fetching display devices: {ex.Message}");
            return null;
        }
    }

    public async Task<DisplayDevicesGetByIdResponse?> GetDisplayDeviceByIdAsync(int id)
    {
        try
        {
            var response = await _apiClient.GetAsync<ApiResponse<DisplayDevicesGetByIdResponse>>($"api/displaydevices/{id}");
            
            if (response != null && response.Success && response.Data != null)
            {
                return response.Data;
            }
            
            return null;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error fetching display device: {ex.Message}");
            return null;
        }
    }

    public async Task<DisplayDevicesCreateResponse?> CreateDeviceAsync(DisplayDevicesCreateRequest request)
    {
        try
        {
            var response = await _apiClient.PostAsync<ApiResponse<DisplayDevicesCreateResponse>>("api/displaydevices", request);
            
            if (response != null && response.Success && response.Data != null)
            {
                return response.Data;
            }
            
            return null;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error creating display device: {ex.Message}");
            return null;
        }
    }
}