using API.Authorization;
using Core.Interfaces;
using Hgs.Share.Requests.CriticalLogs;
using Hgs.Share.Responses.ApiResponses;
using Hgs.Share.Responses.CriticalLogs;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ApiController]
[Route("api/[controller]")]
[MenuPermission("CRITICALLOGS")]
public class CriticalLogsController : ControllerBase
{
    private readonly ICriticalLogService _criticalLogService;

    public CriticalLogsController(ICriticalLogService criticalLogService)
    {
        _criticalLogService = criticalLogService;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResponse<CriticalLogsGetAllResponse>>>> GetFiltered(
        [FromQuery] CriticalLogsFilterRequest request,
        CancellationToken cancellationToken = default)
    {
        // Không dùng try/catch — ExceptionHandlingMiddleware xử lý toàn cục (theo convention repo).
        var result = await _criticalLogService.GetFilteredAsync(request, cancellationToken);
        return Ok(ApiResponse<PagedResponse<CriticalLogsGetAllResponse>>.SuccessResponse(result, "OK", 200));
    }

    [HttpGet("{id:long}")]
    public async Task<ActionResult<ApiResponse<CriticalLogsGetAllResponse>>> GetById(
        long id,
        CancellationToken cancellationToken = default)
    {
        var log = await _criticalLogService.GetByIdAsync(id, cancellationToken);
        if (log is null)
        {
            return NotFound(ApiResponse<CriticalLogsGetAllResponse>.FailResponse("Không tìm thấy nhật ký lỗi", 404));
        }

        return Ok(ApiResponse<CriticalLogsGetAllResponse>.SuccessResponse(log, "OK", 200));
    }
}
