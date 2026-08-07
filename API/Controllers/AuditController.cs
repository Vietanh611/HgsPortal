using Core.Interfaces;
using Hgs.Share.Responses.ApiResponses;
using Hgs.Share.Responses.AuditLogs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AuditController : ControllerBase
{
    private readonly IAuditLogService _auditLogService;
    private readonly ILogger<AuditController> _logger;

    public AuditController(IAuditLogService auditLogService, ILogger<AuditController> logger)
    {
        _auditLogService = auditLogService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResponse<AuditLogsGetAllResponse>>>> GetAll(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var (items, totalCount) = await _auditLogService.GetAllAsync(pageNumber, pageSize, cancellationToken);

            var response = new PagedResponse<AuditLogsGetAllResponse>
            {
                Items = items,
                TotalCount = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalPages = (int)Math.Ceiling((double)totalCount / pageSize)
            };

            return Ok(ApiResponse<PagedResponse<AuditLogsGetAllResponse>>.SuccessResponse(response, "Audit logs retrieved successfully", 200));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving audit logs");
            return StatusCode(500, ApiResponse<PagedResponse<AuditLogsGetAllResponse>>.FailResponse("Error retrieving audit logs", 500));
        }
    }
}