using API.Authorization;
using Core.Interfaces.Operations;
using Hgs.Share.Exceptions;
using Hgs.Share.Requests.Devices;
using Hgs.Share.Responses.ApiResponses;
using Hgs.Share.Responses.Devices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Security.Claims;

namespace API.Controllers;

[ApiController]
[Route("api/[controller]")]
[MenuPermission("DEVICES")]
public class DevicesController : ControllerBase
{
    private readonly IDevicesService _devicesService;
    private readonly ILogger<DevicesController> _logger;

    public DevicesController(IDevicesService devicesService, ILogger<DevicesController> logger)
    {
        _devicesService = devicesService;
        _logger = logger;
    }

    private int? CurrentUserId
    {
        get
        {
            var claimValue = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value;
            return int.TryParse(claimValue, out var userId) ? userId : null;
        }
    }

    /// <summary>
    /// Tạo mã ghép cho thiết bị mới (trạng thái PENDING); mã chỉ trả một lần và hết hạn
    /// theo cấu hình PairingCodeTtlMinutes.
    /// </summary>
    [HttpPost("pairing-code")]
    public async Task<ActionResult<ApiResponse<DevicePairingCodeCreateResponse>>> CreatePairingCode([FromBody] DevicePairingCodeCreateRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var response = await _devicesService.CreatePairingCodeAsync(request, CurrentUserId, cancellationToken);
            _logger.LogInformation("Created pairing code for device '{DeviceName}' ({DeviceIdentifier}).", response.DeviceName, response.DeviceIdentifier);
            return Ok(ApiResponse<DevicePairingCodeCreateResponse>.SuccessResponse(response, "Pairing code created successfully", 201));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ApiResponse<DevicePairingCodeCreateResponse>.FailResponse(ex.Message, 400));
        }
        catch (UnauthorizedAccessException)
        {
            throw;
        }
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<IEnumerable<DeviceGetAllResponse>>>> GetDevices(
        [FromQuery] string? status,
        [FromQuery] int? organizationUnitId,
        CancellationToken cancellationToken)
    {
        var devices = await _devicesService.GetAllAsync(status, organizationUnitId, cancellationToken);
        return Ok(ApiResponse<IEnumerable<DeviceGetAllResponse>>.SuccessResponse(devices, "Devices retrieved successfully", 200));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ApiResponse<DeviceGetByIdResponse>>> GetDeviceById(int id, CancellationToken cancellationToken)
    {
        var device = await _devicesService.GetByIdAsync(id, cancellationToken);
        if (device is null)
        {
            return NotFound(ApiResponse<DeviceGetByIdResponse>.FailResponse("Device not found", 404));
        }

        return Ok(ApiResponse<DeviceGetByIdResponse>.SuccessResponse(device, "Device retrieved successfully", 200));
    }

    /// <summary>
    /// Bật/tắt thiết bị (IsEnabled); từ chối (409) khi thiết bị đã bị thu hồi.
    /// </summary>
    [HttpPatch("{id:int}/status")]
    public async Task<ActionResult<ApiResponse<DeviceStatusUpdateResponse>>> UpdateDeviceStatus(int id, [FromBody] DeviceStatusUpdateRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var response = await _devicesService.UpdateStatusAsync(id, request.IsEnabled, CurrentUserId, cancellationToken);
            return Ok(ApiResponse<DeviceStatusUpdateResponse>.SuccessResponse(response, "Device status updated successfully", 200));
        }
        catch (NotFoundException ex)
        {
            return NotFound(ApiResponse<DeviceStatusUpdateResponse>.FailResponse(ex.Message, 404));
        }
        catch (BusinessRuleException ex)
        {
            return Conflict(ApiResponse<DeviceStatusUpdateResponse>.FailResponse(ex.Message, 409));
        }
    }

    /// <summary>
    /// Thu hồi thiết bị (mềm): chuyển trạng thái REVOKED và tắt thiết bị; chặn (409) nếu đã
    /// thu hồi trước đó.
    /// </summary>
    [HttpPost("{id:int}/revoke")]
    public async Task<ActionResult<ApiResponse<DeviceRevokeResponse>>> RevokeDevice(int id, CancellationToken cancellationToken)
    {
        try
        {
            var response = await _devicesService.RevokeAsync(id, CurrentUserId, cancellationToken);
            _logger.LogWarning("Revoked device '{DeviceName}'.", response.DeviceName);
            return Ok(ApiResponse<DeviceRevokeResponse>.SuccessResponse(response, "Device revoked successfully", 200));
        }
        catch (NotFoundException ex)
        {
            return NotFound(ApiResponse<DeviceRevokeResponse>.FailResponse(ex.Message, 404));
        }
        catch (BusinessRuleException ex)
        {
            return Conflict(ApiResponse<DeviceRevokeResponse>.FailResponse(ex.Message, 409));
        }
    }

    /// <summary>
    /// Tạo mã ghép mới; từ chối (409) khi thiết bị đang ACTIVE (đã ghép) — chỉ tạo lại cho
    /// thiết bị chưa ghép.
    /// </summary>
    [HttpPost("{id:int}/regenerate-pairing-code")]
    public async Task<ActionResult<ApiResponse<DevicePairingCodeRegenerateResponse>>> RegeneratePairingCode(int id, CancellationToken cancellationToken)
    {
        try
        {
            var response = await _devicesService.RegeneratePairingCodeAsync(id, CurrentUserId, cancellationToken);
            _logger.LogWarning("Regenerated pairing code for device '{DeviceName}'.", response.DeviceName);
            return Ok(ApiResponse<DevicePairingCodeRegenerateResponse>.SuccessResponse(response, "Pairing code regenerated successfully", 200));
        }
        catch (NotFoundException ex)
        {
            return NotFound(ApiResponse<DevicePairingCodeRegenerateResponse>.FailResponse(ex.Message, 404));
        }
        catch (BusinessRuleException ex)
        {
            return Conflict(ApiResponse<DevicePairingCodeRegenerateResponse>.FailResponse(ex.Message, 409));
        }
    }

    /// <summary>
    /// Xóa mềm thiết bị; chỉ cho phép khi thiết bị đã bị thu hồi (chưa thu hồi → 409).
    /// </summary>
    [HttpDelete("{id:int}")]
    public async Task<ActionResult<ApiResponse>> DeleteDevice(int id, CancellationToken cancellationToken)
    {
        try
        {
            await _devicesService.DeleteAsync(id, CurrentUserId, cancellationToken);
            return Ok(ApiResponse.SuccessResponse("Device deleted successfully", 200));
        }
        catch (NotFoundException ex)
        {
            return NotFound(ApiResponse.FailResponse(ex.Message, 404));
        }
        catch (BusinessRuleException ex)
        {
            return Conflict(ApiResponse.FailResponse(ex.Message, 409));
        }
    }

    /// <summary>
    /// Ghép thiết bị kiosk bằng mã ghép: ẩn danh, bỏ qua antiforgery, rate limit riêng
    /// (DevicePairing, 5 req/phút/IP); trả DeviceKey chỉ một lần, mã sai/hết hạn → 400.
    /// </summary>
    [HttpPost("pair")]
    [AllowAnonymous]
    [IgnoreAntiforgeryToken]
    [EnableRateLimiting("DevicePairing")]
    public async Task<ActionResult<ApiResponse<DevicePairResponse>>> PairDevice([FromBody] DevicePairRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
            var response = await _devicesService.PairDeviceAsync(request.PairingCode, ipAddress, cancellationToken);
            _logger.LogInformation("Paired device '{DeviceName}' ({DeviceId}).", response.DeviceName, response.DeviceId);
            return Ok(ApiResponse<DevicePairResponse>.SuccessResponse(response, "Device paired successfully", 200));
        }
        catch (BadRequestException ex)
        {
            return BadRequest(ApiResponse<DevicePairResponse>.FailResponse(ex.Message, 400));
        }
    }
}