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
    public const string HeaderDeviceId = "X-Device-Id";
    public const string HeaderDeviceKey = "X-Device-Key";
    public const string DeviceIdClaim = "device_id";
    public const string AuthenticatedDeviceIdKey = "AuthenticatedDeviceId";
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
