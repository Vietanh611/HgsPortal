using Core.Interfaces.Operations;
using Hgs.Share.Dtos;
using Hgs.Share.Exceptions;
using Hgs.Share.Responses.ApiResponses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [EnableRateLimiting("device")]
    public class DisplayController : Controller
    {
        private readonly IDisplayService _displayService;
        public DisplayController(IDisplayService displayService)
        {
            _displayService = displayService;
        }
        /// <summary>
        /// Dữ liệu màn hình hành lý nội địa phục vụ hai đối tượng: kiosk headless (scheme DeviceKey,
        /// header X-Device-Id/X-Device-Key) hiển thị công cộng và user WebApp đăng nhập (JWT Bearer)
        /// xem trước màn hình từ trang display. Dữ liệu là thông tin chuyến bay công khai trên màn
        /// hình sân bay nên không gắn menu permission cho endpoint; việc xác thực (device hợp lệ hoặc
        /// user đã đăng nhập) do các scheme này đảm nhận.
        /// </summary>
        [HttpGet("GetDomesticBaggageArrivalDisplay")]
        [Authorize(AuthenticationSchemes = "DeviceKey,Bearer")]
        public async Task<ActionResult<ApiResponse<List<BaggageArrivalDisplayDto>>>> GetDomesticBaggageArrivalDisplay(CancellationToken cancellationToken)
        {
            var display = await _displayService.GetDomesticBaggageArrivalDisplayAsync(cancellationToken);
            if (display is null)
            {
                throw new NotFoundException($"Display not found");
            }
            return Ok(ApiResponse<List<BaggageArrivalDisplayDto>>.SuccessResponse(display));
        }
        /// <summary>
        /// Dữ liệu màn hình hành lý quốc tế; cùng mục đích và cơ chế xác thực (DeviceKey hoặc JWT)
        /// với endpoint nội địa để kiosk hiển thị công cộng và user đăng nhập xem trước.
        /// </summary>
        [HttpGet("GetInternationalBaggageArrivalDisplay")]
        [Authorize(AuthenticationSchemes = "DeviceKey,Bearer")]
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
