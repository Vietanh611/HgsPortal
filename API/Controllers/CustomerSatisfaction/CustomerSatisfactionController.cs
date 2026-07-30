using Core.Interfaces;
using Domain.Entities.CustomerSatisfaction;
using Hgs.Share.Requests.CustomerSatisfaction;
using Hgs.Share.Responses.ApiResponses;
using Hgs.Share.Responses.CustomerSatisfaction;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers.CustomerSatisfaction;

[Route("api/[controller]")]
[ApiController]
public class CustomerSatisfactionController : ControllerBase
{
    private readonly ICustomerSatisfactionService _customerSatisfactionService;
    private readonly ILogger<CustomerSatisfactionController> _logger;

    public CustomerSatisfactionController(ICustomerSatisfactionService customerSatisfactionService, ILogger<CustomerSatisfactionController> logger)
    {
        _customerSatisfactionService = customerSatisfactionService;
        _logger = logger;
    }

    [HttpGet("devices")]
    public async Task<ActionResult<ApiResponse<IEnumerable<DevicesGetAllResponse>>>> GetDevices()
    {
        var devices = await _customerSatisfactionService.GetDevicesAsync();
        var response = devices.Select(MapToGetAllDeviceResponse).ToList();
        return Ok(ApiResponse<IEnumerable<DevicesGetAllResponse>>.SuccessResponse(response, "Devices retrieved successfully", 200));
    }

    [HttpGet("devices/{id:int}")]
    public async Task<ActionResult<ApiResponse<DevicesGetByIdResponse>>> GetDeviceById(int id)
    {
        var device = await _customerSatisfactionService.GetDeviceByIdAsync(id);
        if (device is null)
        {
            return NotFound(ApiResponse<DevicesGetByIdResponse>.FailResponse("Device not found", 404));
        }

        return Ok(ApiResponse<DevicesGetByIdResponse>.SuccessResponse(MapToGetByIdDeviceResponse(device), "Device retrieved successfully", 200));
    }

    [HttpPost("devices")]
    public async Task<ActionResult<ApiResponse<DevicesCreateResponse>>> CreateDevice([FromBody] DevicesCreateRequest request)
    {
        try
        {
            var device = await _customerSatisfactionService.CreateDeviceAsync(request);
            _logger.LogInformation("Created device '{DeviceIdentifier}'.", device.DeviceIdentifier);
            return Ok(ApiResponse<DevicesCreateResponse>.SuccessResponse(MapToCreateDeviceResponse(device), "Device created successfully", 201));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ApiResponse<DevicesCreateResponse>.FailResponse(ex.Message, 400));
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(ApiResponse<DevicesCreateResponse>.FailResponse(ex.Message, 409));
        }
    }

    [HttpPut("devices/{id:int}")]
    public async Task<ActionResult<ApiResponse<DevicesUpdateResponse>>> UpdateDevice(int id, [FromBody] DevicesUpdateRequest request)
    {
        try
        {
            var device = await _customerSatisfactionService.UpdateDeviceAsync(id, request);
            if (device is null)
            {
                return NotFound(ApiResponse<DevicesUpdateResponse>.FailResponse("Device not found", 404));
            }

            _logger.LogInformation("Updated device '{DeviceIdentifier}'.", device.DeviceIdentifier);
            return Ok(ApiResponse<DevicesUpdateResponse>.SuccessResponse(MapToUpdateDeviceResponse(device), "Device updated successfully", 200));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ApiResponse<DevicesUpdateResponse>.FailResponse(ex.Message, 400));
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(ApiResponse<DevicesUpdateResponse>.FailResponse(ex.Message, 409));
        }
    }

    [HttpDelete("devices/{id:int}")]
    public async Task<ActionResult<ApiResponse>> DeleteDevice(int id)
    {
        try
        {
            var deleted = await _customerSatisfactionService.DeleteDeviceAsync(id);
            if (!deleted)
            {
                return NotFound(ApiResponse.FailResponse("Device not found", 404));
            }

            return Ok(ApiResponse.SuccessResponse("Device deleted successfully", 200));
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(ApiResponse.FailResponse(ex.Message, 409));
        }
    }

    [HttpGet("reasons")]
    public async Task<ActionResult<ApiResponse<IEnumerable<UnsatisfiedReasonsGetAllResponse>>>> GetReasons()
    {
        var reasons = await _customerSatisfactionService.GetReasonsAsync();
        var response = reasons.Select(MapToGetAllReasonResponse).ToList();
        return Ok(ApiResponse<IEnumerable<UnsatisfiedReasonsGetAllResponse>>.SuccessResponse(response, "Reasons retrieved successfully", 200));
    }

    [HttpGet("reasons/{id:int}")]
    public async Task<ActionResult<ApiResponse<UnsatisfiedReasonsGetByIdResponse>>> GetReasonById(int id)
    {
        var reason = await _customerSatisfactionService.GetReasonByIdAsync(id);
        if (reason is null)
        {
            return NotFound(ApiResponse<UnsatisfiedReasonsGetByIdResponse>.FailResponse("Reason not found", 404));
        }

        return Ok(ApiResponse<UnsatisfiedReasonsGetByIdResponse>.SuccessResponse(MapToGetByIdReasonResponse(reason), "Reason retrieved successfully", 200));
    }

    [HttpPost("reasons")]
    public async Task<ActionResult<ApiResponse<UnsatisfiedReasonsCreateResponse>>> CreateReason([FromBody] UnsatisfiedReasonsCreateRequest request)
    {
        try
        {
            var reason = await _customerSatisfactionService.CreateReasonAsync(request);
            _logger.LogInformation("Created reason '{ReasonName}'.", reason.ReasonName);
            return Ok(ApiResponse<UnsatisfiedReasonsCreateResponse>.SuccessResponse(MapToCreateReasonResponse(reason), "Reason created successfully", 201));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ApiResponse<UnsatisfiedReasonsCreateResponse>.FailResponse(ex.Message, 400));
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(ApiResponse<UnsatisfiedReasonsCreateResponse>.FailResponse(ex.Message, 409));
        }
    }

    [HttpPut("reasons/{id:int}")]
    public async Task<ActionResult<ApiResponse<UnsatisfiedReasonsUpdateResponse>>> UpdateReason(int id, [FromBody] UnsatisfiedReasonsUpdateRequest request)
    {
        try
        {
            var reason = await _customerSatisfactionService.UpdateReasonAsync(id, request);
            if (reason is null)
            {
                return NotFound(ApiResponse<UnsatisfiedReasonsUpdateResponse>.FailResponse("Reason not found", 404));
            }

            _logger.LogInformation("Updated reason '{ReasonName}'.", reason.ReasonName);
            return Ok(ApiResponse<UnsatisfiedReasonsUpdateResponse>.SuccessResponse(MapToUpdateReasonResponse(reason), "Reason updated successfully", 200));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ApiResponse<UnsatisfiedReasonsUpdateResponse>.FailResponse(ex.Message, 400));
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(ApiResponse<UnsatisfiedReasonsUpdateResponse>.FailResponse(ex.Message, 409));
        }
    }

    [HttpDelete("reasons/{id:int}")]
    public async Task<ActionResult<ApiResponse>> DeleteReason(int id)
    {
        try
        {
            var deleted = await _customerSatisfactionService.DeleteReasonAsync(id);
            if (!deleted)
            {
                return NotFound(ApiResponse.FailResponse("Reason not found", 404));
            }

            return Ok(ApiResponse.SuccessResponse("Reason deleted successfully", 200));
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(ApiResponse.FailResponse(ex.Message, 409));
        }
    }

    [HttpGet("evaluations")]
    public async Task<ActionResult<ApiResponse<IEnumerable<EvaluationsGetAllResponse>>>> GetEvaluations()
    {
        var evaluations = await _customerSatisfactionService.GetEvaluationsAsync();
        var response = evaluations.Select(MapToGetAllEvaluationResponse).ToList();
        return Ok(ApiResponse<IEnumerable<EvaluationsGetAllResponse>>.SuccessResponse(response, "Evaluations retrieved successfully", 200));
    }

    [HttpGet("evaluations/{id:int}")]
    public async Task<ActionResult<ApiResponse<EvaluationsGetByIdResponse>>> GetEvaluationById(int id)
    {
        var evaluation = await _customerSatisfactionService.GetEvaluationByIdAsync(id);
        if (evaluation is null)
        {
            return NotFound(ApiResponse<EvaluationsGetByIdResponse>.FailResponse("Evaluation not found", 404));
        }

        return Ok(ApiResponse<EvaluationsGetByIdResponse>.SuccessResponse(MapToGetByIdEvaluationResponse(evaluation), "Evaluation retrieved successfully", 200));
    }

    [HttpPost("evaluations")]
    public async Task<ActionResult<ApiResponse<EvaluationsCreateResponse>>> CreateEvaluation([FromBody] EvaluationsCreateRequest request)
    {
        try
        {
            var evaluation = await _customerSatisfactionService.CreateEvaluationAsync(request);
            _logger.LogInformation("Created evaluation '{Id}'.", evaluation.Id);
            return Ok(ApiResponse<EvaluationsCreateResponse>.SuccessResponse(MapToCreateEvaluationResponse(evaluation), "Evaluation created successfully", 201));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ApiResponse<EvaluationsCreateResponse>.FailResponse(ex.Message, 400));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponse<EvaluationsCreateResponse>.FailResponse(ex.Message, 404));
        }
    }

    [HttpPut("evaluations/{id:int}")]
    public async Task<ActionResult<ApiResponse<EvaluationsUpdateResponse>>> UpdateEvaluation(int id, [FromBody] EvaluationsUpdateRequest request)
    {
        try
        {
            var evaluation = await _customerSatisfactionService.UpdateEvaluationAsync(id, request);
            if (evaluation is null)
            {
                return NotFound(ApiResponse<EvaluationsUpdateResponse>.FailResponse("Evaluation not found", 404));
            }

            _logger.LogInformation("Updated evaluation '{Id}'.", evaluation.Id);
            return Ok(ApiResponse<EvaluationsUpdateResponse>.SuccessResponse(MapToUpdateEvaluationResponse(evaluation), "Evaluation updated successfully", 200));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ApiResponse<EvaluationsUpdateResponse>.FailResponse(ex.Message, 400));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponse<EvaluationsUpdateResponse>.FailResponse(ex.Message, 404));
        }
    }

    [HttpDelete("evaluations/{id:int}")]
    public async Task<ActionResult<ApiResponse>> DeleteEvaluation(int id)
    {
        try
        {
            var deleted = await _customerSatisfactionService.DeleteEvaluationAsync(id);
            if (!deleted)
            {
                return NotFound(ApiResponse.FailResponse("Evaluation not found", 404));
            }

            return Ok(ApiResponse.SuccessResponse("Evaluation deleted successfully", 200));
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(ApiResponse.FailResponse(ex.Message, 409));
        }
    }

    private static DevicesGetAllResponse MapToGetAllDeviceResponse(Devices device) => new()
    {
        Id = device.Id,
        DeviceName = device.DeviceName,
        DeviceIdentifier = device.DeviceIdentifier,
        Status = device.Status,
        LastSeenAt = device.LastSeenAt
    };

    private static DevicesGetByIdResponse MapToGetByIdDeviceResponse(Devices device) => new()
    {
        Id = device.Id,
        DeviceName = device.DeviceName,
        DeviceIdentifier = device.DeviceIdentifier,
        Status = device.Status,
        LastSeenAt = device.LastSeenAt
    };

    private static DevicesCreateResponse MapToCreateDeviceResponse(Devices device) => new()
    {
        Id = device.Id,
        DeviceName = device.DeviceName,
        DeviceIdentifier = device.DeviceIdentifier,
        Status = device.Status,
        LastSeenAt = device.LastSeenAt
    };

    private static DevicesUpdateResponse MapToUpdateDeviceResponse(Devices device) => new()
    {
        Id = device.Id,
        DeviceName = device.DeviceName,
        DeviceIdentifier = device.DeviceIdentifier,
        Status = device.Status,
        LastSeenAt = device.LastSeenAt
    };

    private static UnsatisfiedReasonsGetAllResponse MapToGetAllReasonResponse(UnsatisfiedReasons reason) => new()
    {
        Id = reason.Id,
        ReasonName = reason.ReasonName,
        Status = reason.Status
    };

    private static UnsatisfiedReasonsGetByIdResponse MapToGetByIdReasonResponse(UnsatisfiedReasons reason) => new()
    {
        Id = reason.Id,
        ReasonName = reason.ReasonName,
        Status = reason.Status
    };

    private static UnsatisfiedReasonsCreateResponse MapToCreateReasonResponse(UnsatisfiedReasons reason) => new()
    {
        Id = reason.Id,
        ReasonName = reason.ReasonName,
        Status = reason.Status
    };

    private static UnsatisfiedReasonsUpdateResponse MapToUpdateReasonResponse(UnsatisfiedReasons reason) => new()
    {
        Id = reason.Id,
        ReasonName = reason.ReasonName,
        Status = reason.Status
    };

    private static EvaluationsGetAllResponse MapToGetAllEvaluationResponse(Evaluations evaluation) => new()
    {
        Id = evaluation.Id,
        FlightId = evaluation.FlightId,
        StaffUserId = evaluation.StaffUserId,
        DeviceId = evaluation.DeviceId,
        DeviceName = evaluation.Device?.DeviceName,
        CheckinCounterName = evaluation.CheckinCounterName,
        RatingLevel = evaluation.RatingLevel,
        EvaluationType = evaluation.EvaluationType,
        ReasonIds = evaluation.EvaluationReasonLinks.Select(x => x.ReasonId).ToList()
    };

    private static EvaluationsGetByIdResponse MapToGetByIdEvaluationResponse(Evaluations evaluation) => new()
    {
        Id = evaluation.Id,
        FlightId = evaluation.FlightId,
        StaffUserId = evaluation.StaffUserId,
        DeviceId = evaluation.DeviceId,
        DeviceName = evaluation.Device?.DeviceName,
        CheckinCounterName = evaluation.CheckinCounterName,
        RatingLevel = evaluation.RatingLevel,
        EvaluationType = evaluation.EvaluationType,
        ReasonIds = evaluation.EvaluationReasonLinks.Select(x => x.ReasonId).ToList()
    };

    private static EvaluationsCreateResponse MapToCreateEvaluationResponse(Evaluations evaluation) => new()
    {
        Id = evaluation.Id,
        FlightId = evaluation.FlightId,
        StaffUserId = evaluation.StaffUserId,
        DeviceId = evaluation.DeviceId,
        CheckinCounterName = evaluation.CheckinCounterName,
        RatingLevel = evaluation.RatingLevel,
        EvaluationType = evaluation.EvaluationType,
        ReasonIds = evaluation.EvaluationReasonLinks.Select(x => x.ReasonId).ToList()
    };

    private static EvaluationsUpdateResponse MapToUpdateEvaluationResponse(Evaluations evaluation) => new()
    {
        Id = evaluation.Id,
        FlightId = evaluation.FlightId,
        StaffUserId = evaluation.StaffUserId,
        DeviceId = evaluation.DeviceId,
        CheckinCounterName = evaluation.CheckinCounterName,
        RatingLevel = evaluation.RatingLevel,
        EvaluationType = evaluation.EvaluationType,
        ReasonIds = evaluation.EvaluationReasonLinks.Select(x => x.ReasonId).ToList()
    };
}
