using Hgs.Share.Responses.ApiResponses;
using Hgs.Share.Responses.Notifications;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using WebApp.Client.Services.Auth;
using WebApp.Client.Services.Data;
using WebApp.Client.Services.Network;

namespace WebApp.Client.Services.Notification;

/// <summary>
/// Poll nền cho chuông thông báo: mỗi 30 giây lấy số thông báo chưa đọc + 5 dòng gần nhất,
/// bắn <see cref="StateChanged"/> để component đăng ký cập nhật giao diện.
/// Được MainLayout khởi động/dừng theo vòng đời đăng nhập (chỉ chạy trong phiên xác thực).
/// Xử lý lỗi tập trung, theo đúng spec:
/// - 401 → refresh 1 lần rồi thử lại đúng 1 lần.
/// - Phiên hết hạn thật (SessionExpired) → xóa token, dừng poll, về trang đăng nhập.
/// - Lỗi mạng/server (NetworkError) → bỏ qua chu kỳ này, chu kỳ sau thử lại (KHÔNG đăng xuất).
/// </summary>
public class NotificationPollingService : IAsyncDisposable
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(30);

    private readonly ApiClient _apiClient;
    private readonly TokenRefreshService _tokenRefreshService;
    private readonly AuthenticationStateProvider _authenticationStateProvider;
    private readonly Data.ITokenStorage _tokenStorage;
    private readonly NavigationManager _navigation;

    private PeriodicTimer? _timer;
    private CancellationTokenSource? _cts;
    private int _polling;

    /// <summary>Bắn sau mỗi lần poll thành công (dữ liệu có thể không đổi).</summary>
    public event Action? StateChanged;

    public int UnreadCount { get; private set; }

    public List<NotificationListItemResponse> Notifications { get; private set; } = new();

    public NotificationPollingService(
        ApiClient apiClient,
        TokenRefreshService tokenRefreshService,
        AuthenticationStateProvider authenticationStateProvider,
        Data.ITokenStorage tokenStorage,
        NavigationManager navigation)
    {
        _apiClient = apiClient;
        _tokenRefreshService = tokenRefreshService;
        _authenticationStateProvider = authenticationStateProvider;
        _tokenStorage = tokenStorage;
        _navigation = navigation;
    }

    /// <summary>
    /// Bắt đầu poll. Idempotent: Start trùng lặp (login lại, sự kiện auth trùng) bị bỏ qua.
    /// </summary>
    public void Start()
    {
        if (Interlocked.Exchange(ref _polling, 1) == 1)
        {
            return;
        }

        _cts = new CancellationTokenSource();
        _timer = new PeriodicTimer(PollInterval);
        _ = PollLoopAsync(_cts.Token);
    }

    /// <summary>Dừng poll (logout / layout dispose). Idempotent.</summary>
    public void Stop()
    {
        if (Interlocked.Exchange(ref _polling, 0) == 0)
        {
            return;
        }

        _cts?.Cancel();
        _timer?.Dispose();
        _timer = null;
    }

    private async Task PollLoopAsync(CancellationToken ct)
    {
        try
        {
            // Poll ngay khi khởi động để chuông có dữ liệu tức thì, không phải đợi 30 giây.
            await PollOnceAsync(ct);

            while (await _timer!.WaitForNextTickAsync(ct))
            {
                await PollOnceAsync(ct);
            }
        }
        catch (OperationCanceledException)
        {
            // Dừng bình thường khi Stop()/logout.
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Notification polling loop error: {ex.Message}");
        }
    }

    private async Task PollOnceAsync(CancellationToken ct)
    {
        try
        {
            var countResult = await _apiClient.GetSilentAsync<ApiResponse<int>>("api/notifications/unread-count");
            var listResult = await _apiClient.GetSilentAsync<ApiResponse<PagedResponse<NotificationListItemResponse>>>("api/notifications?pageSize=5");

            // 401 → refresh 1 lần rồi thử lại đúng 1 lần.
            if (countResult.IsUnauthorized || listResult.IsUnauthorized)
            {
                switch (await _tokenRefreshService.RefreshTokenAsync())
                {
                    case TokenRefreshResult.SessionExpired:
                        // Phiên thực sự hết hạn: dọn token, báo auth state, về trang đăng nhập.
                        Console.WriteLine("Notification polling: session expired, redirecting to login");
                        await _tokenStorage.ClearTokensAsync();
                        if (_authenticationStateProvider is Auth.CustomAuthenticationStateProvider provider)
                        {
                            provider.NotifyAuthenticationStateChanged();
                        }
                        Stop();
                        _navigation.NavigateTo("login", forceLoad: true);
                        return;

                    case TokenRefreshResult.NetworkError:
                        // Lỗi mạng/server tạm thời: bỏ qua chu kỳ này, chu kỳ sau thử lại.
                        Console.WriteLine("Notification polling: refresh network error, skipping cycle");
                        return;

                    case TokenRefreshResult.Success:
                        countResult = await _apiClient.GetSilentAsync<ApiResponse<int>>("api/notifications/unread-count");
                        listResult = await _apiClient.GetSilentAsync<ApiResponse<PagedResponse<NotificationListItemResponse>>>("api/notifications?pageSize=5");
                        break;
                }
            }

            if (countResult.Success && countResult.Data?.Success == true)
            {
                UnreadCount = countResult.Data.Data;
            }

            if (listResult.Success && listResult.Data?.Success == true && listResult.Data.Data != null)
            {
                Notifications = listResult.Data.Data.Items?.ToList() ?? new List<NotificationListItemResponse>();
            }

            if (!ct.IsCancellationRequested)
            {
                StateChanged?.Invoke();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Notification polling error: {ex.Message}");
        }
    }

    public ValueTask DisposeAsync()
    {
        Stop();
        _cts?.Dispose();
        return ValueTask.CompletedTask;
    }
}