using Core.Interfaces.Operations;
using Hgs.Share.Responses.ApiResponses;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Text.Json;

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
    /// <summary>Khóa trong HttpContext.Items lưu lý do xác thực bị từ chối (REVOKED/DISABLED), dùng để ghi body 401 cho kiosk biết cách xử lý.</summary>
    public const string DeviceAuthStatusKey = "DeviceAuthStatus";
    /// <summary>Tiền tố cache key dùng để giới hạn việc ghi LastSeenAt xuống tối đa 1 lần/phút/thiết bị.</summary>
    private const string LastSeenCachePrefix = "device:lastseen:";

    private const string DeviceAuthStatusRevoked = "REVOKED";
    private const string DeviceAuthStatusDisabled = "DISABLED";

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
    /// device_id — cố ý không mang claim user. Khi bị từ chối, lưu lý do (REVOKED/DISABLED)
    /// vào Context.Items để HandleChallengeAsync ghi body 401 với ErrorCode tương ứng.
    /// </summary>
    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var deviceId = Request.Headers[HeaderDeviceId].ToString();
        var deviceKey = Request.Headers[HeaderDeviceKey].ToString();

        if (string.IsNullOrWhiteSpace(deviceId) || string.IsNullOrWhiteSpace(deviceKey))
        {
            return AuthenticateResult.Fail("Missing device credentials");
        }

        var result = await _devicesService.AuthenticateDeviceAsync(deviceId, deviceKey, cancellationToken: Context.RequestAborted);
        if (!result.IsAuthenticated)
        {
            Logger.LogWarning("Device authentication failed for identifier '{DeviceIdentifier}' on {Path} (reason: {Reason}).", deviceId, Context.Request.Path, result.Reason);
            Context.Items[DeviceAuthStatusKey] = result.Reason switch
            {
                DeviceAuthFailureReason.Revoked => DeviceAuthStatusRevoked,
                DeviceAuthFailureReason.Disabled => DeviceAuthStatusDisabled,
                _ => null
            };
            return AuthenticateResult.Fail("Invalid device credentials");
        }

        var device = result.Device!;
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
    /// Ghi body 401 (ApiResponse) kèm ErrorCode DEVICE_REVOKED/DEVICE_DISABLED/DEVICE_INVALID để
    /// kiosk phân biệt thiết bị bị tắt (giữ cấu hình, chờ bật lại) hay bị thu hồi (phải ghép cặp
    /// lại). Với request có Bearer hợp lệ thì scheme không rơi vào nhánh challenge này nên admin
    /// preview luôn nhận 200 và không bị ảnh hưởng.
    /// </summary>
    protected override Task HandleChallengeAsync(AuthenticationProperties properties)
    {
        Response.StatusCode = StatusCodes.Status401Unauthorized;
        Response.ContentType = "application/json; charset=utf-8";

        var status = Context.Items[DeviceAuthStatusKey]?.ToString();
        var (errorCode, message) = status switch
        {
            DeviceAuthStatusRevoked => ("DEVICE_REVOKED", "Thiết bị đã bị thu hồi."),
            DeviceAuthStatusDisabled => ("DEVICE_DISABLED", "Thiết bị đã bị vô hiệu hóa."),
            _ => ("DEVICE_INVALID", "Thiết bị không hợp lệ.")
        };

        var body = new ApiResponse
        {
            Success = false,
            StatusCode = StatusCodes.Status401Unauthorized,
            ErrorCode = errorCode,
            Message = message
        };

        Response.Headers.WWWAuthenticate = Scheme.Name;

        return Response.WriteAsync(JsonSerializer.Serialize(body, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        }));
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
