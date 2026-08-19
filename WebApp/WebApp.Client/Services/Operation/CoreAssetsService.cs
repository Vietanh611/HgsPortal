using Hgs.Share.Requests.CoreAssets;
using Hgs.Share.Responses.ApiResponses;
using Hgs.Share.Responses.CoreAssets;
using Microsoft.AspNetCore.Components;
using WebApp.Client.Services.Network;

namespace WebApp.Client.Services.Operation;

/// <summary>
/// Service phía client cho danh mục core assets; các method trả null khi lỗi để giao diện
/// hiển thị trạng thái mặc định.
/// </summary>
public class CoreAssetsService
{
    private readonly ApiClient _apiClient;
    private readonly NavigationManager _navigationManager;

    public CoreAssetsService(ApiClient apiClient, NavigationManager navigationManager)
    {
        _apiClient = apiClient;
        _navigationManager = navigationManager;
    }

    public async Task<IEnumerable<CoreAssetsGetAllResponse>?> GetCoreAssetsAsync()
    {
        try
        {
            var response = await _apiClient.GetAsync<ApiResponse<IEnumerable<CoreAssetsGetAllResponse>>>("api/coreassets");
            
            if (response != null && response.Success && response.Data != null)
            {
                return response.Data;
            }
            
            return null;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error fetching core assets: {ex.Message}");
            return null;
        }
    }

    public async Task<CoreAssetsGetByIdResponse?> GetCoreAssetByIdAsync(int id)
    {
        try
        {
            var response = await _apiClient.GetAsync<ApiResponse<CoreAssetsGetByIdResponse>>($"api/coreassets/{id}");
            
            if (response != null && response.Success && response.Data != null)
            {
                return response.Data;
            }
            
            return null;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error fetching core asset: {ex.Message}");
            return null;
        }
    }

    public async Task<CoreAssetsCreateResponse?> CreateAssetAsync(CoreAssetsCreateRequest request)
    {
        try
        {
            var response = await _apiClient.PostAsync<ApiResponse<CoreAssetsCreateResponse>>("api/coreassets", request);
            
            if (response != null && response.Success && response.Data != null)
            {
                return response.Data;
            }
            
            return null;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error creating core asset: {ex.Message}");
            return null;
        }
    }

    public async Task<CoreAssetsCreateResponse?> UpdateAssetAsync(int id, CoreAssetsUpdateRequest request)
    {
        try
        {
            var response = await _apiClient.PutAsync<ApiResponse<CoreAssetsCreateResponse>>($"api/coreassets/{id}", request);

            if (response != null && response.Success && response.Data != null)
            {
                return response.Data;
            }

            return null;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error updating core asset: {ex.Message}");
            return null;
        }
    }

    public async Task<bool> DeleteAssetAsync(int id)
    {
        try
        {
            return await _apiClient.DeleteAsync($"api/coreassets/{id}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error deleting core asset: {ex.Message}");
            return false;
        }
    }
}