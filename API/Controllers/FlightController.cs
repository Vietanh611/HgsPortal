using API.Authorization;
using Core.Interfaces.Operations;
using Domain.Entities.ACDM;
using Hgs.Share.Exceptions;
using Hgs.Share.Responses.ApiResponses;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ApiController]
[Route("api/[controller]")]
[MenuPermission("FLIGHT")]
public class FlightController : ControllerBase
{
    private readonly IFlightService _flightService;

    public FlightController(IFlightService flightService)
    {
        _flightService = flightService;
    }

    /// <summary>
    /// Trả về chuyến bay đọc từ cơ sở dữ liệu hệ thống ACDM (AcdmContext), không phải dữ liệu HGS.
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<FlightACDM>>> GetById(int id, CancellationToken cancellationToken)
    {
        var flight = await _flightService.GetByIdAsync(id, cancellationToken);
        if (flight is null)
        {
            throw new NotFoundException($"Flight with ID {id} not found");
        }
        return Ok(ApiResponse<FlightACDM>.SuccessResponse(flight));
    }

    /// <summary>
    /// Tìm chuyến bay theo flightNo/flightDate; thiếu cả hai filter → trả về toàn bộ chuyến bay.
    /// </summary>
    [HttpGet("search")]
    public async Task<ActionResult<ApiResponse<IEnumerable<FlightACDM>>>> Search([FromQuery] string? flightNo, [FromQuery] string? flightDate, CancellationToken cancellationToken)
    {
        var flights = await _flightService.GetByFlightNoAndDateAsync(flightNo, flightDate, cancellationToken);
        return Ok(ApiResponse<IEnumerable<FlightACDM>>.SuccessResponse(flights));
    }

}
