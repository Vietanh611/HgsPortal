using API.Authorization;
using Core.Interfaces.Operations;
using Hgs.Share.Exceptions;
using Hgs.Share.Requests.Audit;
using Hgs.Share.Responses.ApiResponses;
using Hgs.Share.Responses.AuditLogs;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers.System;

[ApiController]
[Route("api/[controller]")]
[MenuPermission("AUDIT")]
public class AuditController : ControllerBase
{
    private readonly IAuditLogService _auditLogService;
    private readonly IAuditLogExportService _auditLogExportService;
    private readonly ILogger<AuditController> _logger;

    public AuditController(
        IAuditLogService auditLogService,
        IAuditLogExportService auditLogExportService,
        ILogger<AuditController> logger)
    {
        _auditLogService = auditLogService;
        _auditLogExportService = auditLogExportService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResponse<AuditLogsGetAllResponse>>>> GetFiltered(
        [FromQuery] AuditLogsFilterRequest request,
        CancellationToken cancellationToken = default)
    {
        // Không dùng try/catch — ExceptionHandlingMiddleware xử lý toàn cục (theo convention repo)
        var result = await _auditLogService.GetFilteredAsync(request, cancellationToken);
        return Ok(ApiResponse<PagedResponse<AuditLogsGetAllResponse>>.SuccessResponse(result, "OK", 200));
    }

    /// <summary>
    /// Xuất audit log ra file CSV/Excel với các ràng buộc: định dạng bắt buộc csv/xlsx,
    /// khoảng thời gian tối đa 90 ngày, tối đa 50.000 dòng; mỗi lần export tự ghi một
    /// dòng audit EXPORT.
    /// </summary>
    /// <remarks>
    /// Không cung cấp fromDate/toDate thì mặc định xuất 30 ngày gần nhất.
    /// </remarks>
    [HttpGet("export")]
    public async Task<IActionResult> Export(
        [FromQuery] AuditLogsFilterRequest request,
        [FromQuery] string format = "xlsx",
        CancellationToken cancellationToken = default)
    {
        if (format is not ("csv" or "xlsx"))
        {
            throw new BadRequestException("format phải là csv hoặc xlsx");
        }

        var fileName = $"audit-log-{DateTime.UtcNow:yyyyMMdd-HHmmss}.{format}";
        var bytes = format == "csv"
            ? await _auditLogExportService.ExportCsvAsync(request, cancellationToken)
            : await _auditLogExportService.ExportExcelAsync(request, cancellationToken);

        var contentType = format == "csv"
            ? "text/csv"
            : "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

        return File(bytes, contentType, fileName);
    }
}