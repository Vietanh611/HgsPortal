using Hgs.Share.Dtos;
using Hgs.Share.Responses.ApiResponses;
using Hgs.Share.Responses.CoreAssets;
using Microsoft.AspNetCore.Components;
using WebApp.Client.Services.Network;

namespace WebApp.Client.Services;

/// <summary>
/// Service dữ liệu cho các trang hiển thị công cộng (baggage arrival display), được truy
/// cập mà không cần đăng nhập.
/// </summary>
public class DisplayClientService
{
    private readonly ApiClient _apiClient;
    private readonly NavigationManager _navigationManager;
    private readonly CoreAssetsService _coreAssetsService;

    public DisplayClientService(ApiClient apiClient, NavigationManager navigationManager, CoreAssetsService coreAssetsService)
    {
        _apiClient = apiClient;
        _navigationManager = navigationManager;
        _coreAssetsService = coreAssetsService;
        Console.WriteLine(
        $"DisplayClientService: ApiClient={_apiClient.GetHashCode()}");
    }

    /// <summary>
    /// Nạp dữ liệu hiển thị chuyến bay đến nội địa; trả null khi API lỗi hoặc không có dữ
    /// liệu để trang xử lý trạng thái trống.
    /// </summary>
    public async Task<List<BaggageArrivalDisplayDto>?> GetDomesticBaggageArrivalDisplayAsync()
    {
        try
        {
            var response = await _apiClient.GetAsync<ApiResponse<IEnumerable<BaggageArrivalDisplayDto>>>("api/display/GetDomesticBaggageArrivalDisplay");

            if (response != null && response.Success && response.Data != null)
            {
                return response.Data.ToList();
            }

            return null;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error fetching domestic baggage arrival display: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Nạp dữ liệu hiển thị chuyến bay đến quốc tế; trả null khi API lỗi hoặc không có dữ
    /// liệu để trang xử lý trạng thái trống.
    /// </summary>
    public async Task<List<BaggageArrivalDisplayDto>?> GetInternationalBaggageArrivalDisplayAsync()
    {
        try
        {
            var response = await _apiClient.GetAsync<ApiResponse<IEnumerable<BaggageArrivalDisplayDto>>>("api/display/GetInternationalBaggageArrivalDisplay");

            if (response != null && response.Success && response.Data != null)
            {
                return response.Data.ToList();
            }

            return null;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error fetching international baggage arrival display: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Lấy các core asset đang hoạt động (IsActive) có Code khớp với assetCode, dùng để
    /// phân loại hiển thị theo loại tài sản.
    /// </summary>
    public async Task<List<CoreAssetsGetAllResponse>?> GetCoreAssetsByTypeAsync(string assetCode)
    {
        try
        {

            var response = await _apiClient.GetAsync<ApiResponse<IEnumerable<CoreAssetsGetAllResponse>>>("api/coreassets");

            var assets = response?.Data?.ToList();

            if (assets != null)
            {
                return assets.Where(a => a.Code == assetCode && a.IsActive).ToList();
            }

            return null;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error fetching core assets by type: {ex.Message}");
            return null;
        }
    }
}