using Hgs.Share.Requests.CoreAssets;
using Hgs.Share.Responses.ApiResponses;
using Hgs.Share.Responses.CoreAssets;
using Microsoft.AspNetCore.Components;
using WebApp.Client.Services.Network;

namespace WebApp.Client.Services;

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
}