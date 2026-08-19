using API.Authorization;
using Core.Interfaces.Operations;
using Hgs.Share.Requests.CriticalLogs;
using Hgs.Share.Responses.ApiResponses;
using Hgs.Share.Responses.CriticalLogs;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers.System;

/// <summary>
/// Đọc nhật ký lỗi hệ thống (bảng CriticalLogs do Serilog ghi) cho trang giám sát admin.
/// Log là dữ liệu nhạy cảm toàn cục, không bị giới hạn theo phạm vi tổ chức — quyền truy cập
/// được chốt bởi menu CRITICALLOGS.
/// </summary>
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

    /// <summary>
    /// Danh sách critical log phân trang + lọc (level/khoảng thời gian/từ khóa); phân trang
    /// được clamp trong service (PageNumber ≥ 1, PageSize ∈ [1, 200]) để chống DoS qua [FromQuery].
    /// </summary>
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
