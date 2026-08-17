using Blazored.LocalStorage;

namespace WebApp.Client.Services;

/// <summary>Thông tin thiết bị kiosk được lưu trong localStorage của trình duyệt kiosk.</summary>
public class KioskDeviceConfig
{
    public string DeviceId { get; set; } = string.Empty;
    public string DeviceKey { get; set; } = string.Empty;
}

/// <summary>
/// Lưu/đọc cấu hình thiết bị kiosk (DeviceId + DeviceKey) dùng cho các trang hiển thị công cộng.
/// Cấu hình được tạo tại kiosk qua quy trình pairing (nhập mã ghép 8 ký tự, server trả DeviceKey 1 lần).
/// </summary>
public class KioskDeviceConfigService
{
    private const string StorageKey = "hgs.kioskDeviceConfig";
    private readonly ILocalStorageService _localStorage;

    public KioskDeviceConfigService(ILocalStorageService localStorage)
    {
        _localStorage = localStorage;
    }

    public async Task<KioskDeviceConfig?> GetAsync()
    {
        try
        {
            var config = await _localStorage.GetItemAsync<KioskDeviceConfig>(StorageKey);
            if (config == null || string.IsNullOrWhiteSpace(config.DeviceId) || string.IsNullOrWhiteSpace(config.DeviceKey))
            {
                return null;
            }

            return config;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error reading kiosk device config: {ex.Message}");
            return null;
        }
    }

    public async Task SaveAsync(KioskDeviceConfig config)
    {
        await _localStorage.SetItemAsync(StorageKey, config);
    }

    public async Task ClearAsync()
    {
        await _localStorage.RemoveItemAsync(StorageKey);
    }
}