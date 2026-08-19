using Data.DbContexts;
using Microsoft.EntityFrameworkCore;

namespace API.Services;

/// <summary>
/// Dọn dẹp thông báo quá hạn (ExpiresAt &lt; hiện tại) theo chính sách retention 30 ngày.
/// Chạy lần đầu sau 30s khởi động rồi lặp mỗi 24h; không dùng CRON để tránh phụ thuộc
/// lịch trình ngoài tiến trình (hosted service duy nhất của hệ thống).
/// </summary>
public class NotificationCleanupService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<NotificationCleanupService> _logger;
    private static readonly TimeSpan StartupDelay = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan CleanupInterval = TimeSpan.FromHours(24);

    public NotificationCleanupService(IServiceScopeFactory scopeFactory, ILogger<NotificationCleanupService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await Task.Delay(StartupDelay, stoppingToken);
        }
        catch (TaskCanceledException)
        {
            return; // Dừng trước khi bắt đầu chu kỳ — không cần làm gì thêm
        }

        using var timer = new PeriodicTimer(CleanupInterval);
        try
        {
            do
            {
                await CleanupExpiredAsync(stoppingToken);
            }
            while (await timer.WaitForNextTickAsync(stoppingToken));
        }
        catch (OperationCanceledException)
        {
            // Shutdown bình thường — dừng vòng lặp
        }
    }

    private async Task CleanupExpiredAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<HgsDbContext>();

            // ExecuteDelete xóa cứng; recipient cascade theo FK ON DELETE CASCADE, TriggeredByUser SetNull
            var deleted = await dbContext.Notifications
                .Where(n => n.ExpiresAt < DateTime.UtcNow)
                .ExecuteDeleteAsync(cancellationToken);

            if (deleted > 0)
            {
                _logger.LogInformation("Dọn dẹp thông báo hết hạn: đã xóa {Count} thông báo", deleted);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi dọn dẹp thông báo hết hạn");
        }
    }
}