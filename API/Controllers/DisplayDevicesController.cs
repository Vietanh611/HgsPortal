using Core.Interfaces;
using Domain.Entities.DisplayDevices;
using Hgs.Share.Requests.DisplayDevices;
using Hgs.Share.Responses.ApiResponses;
using Hgs.Share.Responses.DisplayDevices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DisplayDevicesController : ControllerBase
{
    private readonly IDisplayDevicesService _displayDevicesService;
    private readonly ILogger<DisplayDevicesController> _logger;

    public DisplayDevicesController(IDisplayDevicesService displayDevicesService, ILogger<DisplayDevicesController> logger)
    {
        _displayDevicesService = displayDevicesService;
        _logger = logger;
    }

    [HttpGet]
    [Authorize]
    public async Task<ActionResult<ApiResponse<IEnumerable<DisplayDevicesGetAllResponse>>>> GetDisplayDevices(CancellationToken cancellationToken)
    {
        var devices = await _displayDevicesService.GetDisplayDevicesAsync(cancellationToken);
        var response = devices.Select(MapToGetAllResponse).ToList();
        return Ok(ApiResponse<IEnumerable<DisplayDevicesGetAllResponse>>.SuccessResponse(response, "Display devices retrieved successfully", 200));
    }

    [HttpGet("{id:int}")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<DisplayDevicesGetByIdResponse>>> GetDisplayDeviceById(int id, CancellationToken cancellationToken)
    {
        var device = await _displayDevicesService.GetDisplayDeviceByIdAsync(id, cancellationToken);
        if (device is null)
        {
            return NotFound(ApiResponse<DisplayDevicesGetByIdResponse>.FailResponse("Display device not found", 404));
        }

        return Ok(ApiResponse<DisplayDevicesGetByIdResponse>.SuccessResponse(MapToGetByIdResponse(device), "Display device retrieved successfully", 200));
    }

    [HttpPost]
    [Authorize]
    public async Task<ActionResult<ApiResponse<DisplayDevicesCreateResponse>>> CreateDevice([FromBody] DisplayDevicesCreateRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var device = await _displayDevicesService.CreateDeviceAsync(request, cancellationToken);
            _logger.LogInformation("Created display device '{DeviceIdentifier}'.", device.DeviceIdentifier);
            return Ok(ApiResponse<DisplayDevicesCreateResponse>.SuccessResponse(MapToCreateResponse(device), "Display device created successfully", 201));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ApiResponse<DisplayDevicesCreateResponse>.FailResponse(ex.Message, 400));
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(ApiResponse<DisplayDevicesCreateResponse>.FailResponse(ex.Message, 409));
        }
    }

    private static DisplayDevicesGetAllResponse MapToGetAllResponse(DisplayDevices device) => new()
    {
        Id = device.Id,
        DeviceName = device.DeviceName,
        DeviceIdentifier = device.DeviceIdentifier,
        Status = device.Status,
        LastSeenAt = device.LastSeenAt,
        IsEnabled = device.IsEnabled
    };

    private static DisplayDevicesGetByIdResponse MapToGetByIdResponse(DisplayDevices device) => new()
    {
        Id = device.Id,
        DeviceName = device.DeviceName,
        DeviceIdentifier = device.DeviceIdentifier,
        Status = device.Status,
        LastSeenAt = device.LastSeenAt,
        IsEnabled = device.IsEnabled
    };

    private static DisplayDevicesCreateResponse MapToCreateResponse(DisplayDevices device) => new()
    {
        Id = device.Id,
        DeviceName = device.DeviceName,
        DeviceIdentifier = device.DeviceIdentifier,
        Status = device.Status,
        LastSeenAt = device.LastSeenAt,
        IsEnabled = device.IsEnabled
    };
}