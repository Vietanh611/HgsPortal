using Core.Interfaces;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Text.Encodings.Web;

namespace API.Authorization;

/// <summary>
/// Scheme xác thực thiết bị headless bằng header X-Device-Id + X-Device-Key.
/// Hoạt động song song với JWT Bearer: principal tối thiểu, dùng claim riêng "device_id"
/// (KHÔNG dùng chung NameIdentifier với user) để tránh nhầm lẫn trong code kiểm tra scope.
/// </summary>
public class DeviceKeyAuthenticationHandler : AuthenticationHandler<DeviceKeyAuthenticationSchemeOptions>
{
    /// <summary>Header chứa mã định danh thiết bị (kiosk headless) khi gọi API.</summary>
    public const string HeaderDeviceId = "X-Device-Id";
    /// <summary>Header chứa khóa bí mật của thiết bị (kiosk headless) khi gọi API.</summary>
    public const string HeaderDeviceKey = "X-Device-Key";
    /// <summary>Tên claim mang mã định danh thiết bị — cố ý tách khỏi NameIdentifier của user để code kiểm tra scope không nhầm danh tính thiết bị với danh tính người dùng.</summary>
    public const string DeviceIdClaim = "device_id";
    /// <summary>Khóa trong HttpContext.Items lưu id DB của thiết bị đã xác thực trong suốt request, để middleware phía sau nhận diện thiết bị.</summary>
    public const string AuthenticatedDeviceIdKey = "AuthenticatedDeviceId";
    /// <summary>Tiền tố cache key dùng để giới hạn việc ghi LastSeenAt xuống tối đa 1 lần/phút/thiết bị.</summary>
    private const string LastSeenCachePrefix = "device:lastseen:";

    private readonly IDevicesService _devicesService;
    private readonly ICacheService _cacheService;

    public DeviceKeyAuthenticationHandler(
        IOptionsMonitor<DeviceKeyAuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        IDevicesService devicesService,
        ICacheService cacheService)
        : base(options, logger, encoder)
    {
        _devicesService = devicesService;
        _cacheService = cacheService;
    }

    /// <summary>
    /// Xác thực thiết bị bằng cặp header X-Device-Id/X-Device-Key qua IDevicesService. Khi thành
    /// công, id DB của thiết bị được đưa vào HttpContext.Items và principal chỉ mang claim
    /// device_id — cố ý không mang claim user.
    /// </summary>
    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var deviceId = Request.Headers[HeaderDeviceId].ToString();
        var deviceKey = Request.Headers[HeaderDeviceKey].ToString();

        if (string.IsNullOrWhiteSpace(deviceId) || string.IsNullOrWhiteSpace(deviceKey))
        {
            return AuthenticateResult.Fail("Missing device credentials");
        }

        var device = await _devicesService.AuthenticateDeviceAsync(deviceId, deviceKey, cancellationToken: Context.RequestAborted);
        if (device is null)
        {
            Logger.LogWarning("Device authentication failed for identifier '{DeviceIdentifier}' on {Path}.", deviceId, Context.Request.Path);
            return AuthenticateResult.Fail("Invalid device credentials");
        }

        Context.Items[AuthenticatedDeviceIdKey] = device.Id;
        await TouchLastSeenAsync(device.Id, Context.RequestAborted);

        var claims = new List<Claim>
        {
            new(DeviceIdClaim, device.DeviceIdentifier),
            new(ClaimTypes.AuthenticationMethod, Scheme.Name)
        };

        var identity = new ClaimsIdentity(claims, Scheme.Name);
        var principal = new ClaimsPrincipal(identity);

        return AuthenticateResult.Success(new AuthenticationTicket(principal, Scheme.Name));
    }

    /// <summary>
    /// Cập nhật LastSeenAt/LastSeenIp của thiết bị, nhưng tối đa 1 lần/phút/thiết bị (chặn bằng
    /// cache ngắn hạn) để tránh ghi DB mỗi request.
    /// </summary>
    private async Task TouchLastSeenAsync(int deviceId, CancellationToken cancellationToken)
    {
        var cacheKey = $"{LastSeenCachePrefix}{deviceId}";
        var cached = await _cacheService.GetAsync<string>(cacheKey, cancellationToken);
        if (cached is not null)
        {
            return;
        }

        var ipAddress = Context.Connection.RemoteIpAddress?.ToString();
        await _devicesService.UpdateLastSeenAtAsync(deviceId, ipAddress, cancellationToken);
        await _cacheService.SetAsync(cacheKey, "1", TimeSpan.FromMinutes(1), cancellationToken);
    }
}
