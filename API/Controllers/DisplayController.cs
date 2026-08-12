using Core.Interfaces;
using Hgs.Share.Dtos;
using Hgs.Share.Exceptions;
using Hgs.Share.Responses.ApiResponses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/[controller]")]
    public class DisplayController : Controller
    {
        private readonly IDisplayService _displayService;
        public DisplayController(IDisplayService displayService)
        {
            _displayService = displayService;
        }
        [HttpGet("GetDomesticBaggageArrivalDisplay")]
        [AllowAnonymous]
        public async Task<ActionResult<ApiResponse<List<BaggageArrivalDisplayDto>>>> GetDomesticBaggageArrivalDisplay(CancellationToken cancellationToken)
        {
            var display = await _displayService.GetDomesticBaggageArrivalDisplayAsync(cancellationToken);
            if (display is null)
            {
                throw new NotFoundException($"Display not found");
            }
            return Ok(ApiResponse<List<BaggageArrivalDisplayDto>>.SuccessResponse(display));
        }
        [HttpGet("GetInternationalBaggageArrivalDisplay")]
        [AllowAnonymous]
        public async Task<ActionResult<ApiResponse<List<BaggageArrivalDisplayDto>>>> GetInternationalBaggageArrivalDisplay(CancellationToken cancellationToken)
        {
            var display = await _displayService.GetInternationalBaggageArrivalDisplayAsync(cancellationToken);
            if (display is null)
            {
                throw new NotFoundException($"Display not found");
            }
            return Ok(ApiResponse<List<BaggageArrivalDisplayDto>>.SuccessResponse(display));
        }
    }
}
