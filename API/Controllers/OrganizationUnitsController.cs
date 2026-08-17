using API.Authorization;
using Core.Interfaces;
using Domain.Entities.Identity;
using Hgs.Share.Requests.OrganizationUnits;
using Hgs.Share.Responses.ApiResponses;
using Hgs.Share.Responses.OrganizationUnits;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ApiController]
[Route("api/[controller]")]
[MenuPermission("ORGANIZATIONUNITS", "USERS")]
public class OrganizationUnitsController : ControllerBase
{
    private readonly IOrganizationUnitsService _organizationUnitsService;
    private readonly ILogger<OrganizationUnitsController> _logger;

    public OrganizationUnitsController(IOrganizationUnitsService organizationUnitsService, ILogger<OrganizationUnitsController> logger)
    {
        _organizationUnitsService = organizationUnitsService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<IEnumerable<OrganizationUnitsGetAllResponse>>>> GetAll()
    {
        var organizationUnits = await _organizationUnitsService.GetAllAsync();
        var response = organizationUnits.Select(MapToGetAllResponse).ToList();

        return Ok(ApiResponse<IEnumerable<OrganizationUnitsGetAllResponse>>.SuccessResponse(response, "Organization units retrieved successfully", 200));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ApiResponse<OrganizationUnitsGetByIdResponse>>> GetById(int id)
    {
        var organizationUnit = await _organizationUnitsService.GetByIdAsync(id);
        if (organizationUnit is null)
        {
            return NotFound(ApiResponse<OrganizationUnitsGetByIdResponse>.FailResponse("Organization unit not found", 404));
        }

        return Ok(ApiResponse<OrganizationUnitsGetByIdResponse>.SuccessResponse(MapToGetByIdResponse(organizationUnit), "Organization unit retrieved successfully", 200));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<OrganizationUnitsCreateResponse>>> Create([FromBody] OrganizationUnitsCreateRequest request)
    {
        try
        {
            var organizationUnit = await _organizationUnitsService.CreateAsync(request);
            _logger.LogInformation("Created organization unit '{Code}'.", organizationUnit.Code);
            return Ok(ApiResponse<OrganizationUnitsCreateResponse>.SuccessResponse(MapToCreateResponse(organizationUnit), "Organization unit created successfully", 201));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ApiResponse<OrganizationUnitsCreateResponse>.FailResponse(ex.Message, 400));
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(ApiResponse<OrganizationUnitsCreateResponse>.FailResponse(ex.Message, 409));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponse<OrganizationUnitsCreateResponse>.FailResponse(ex.Message, 404));
        }
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<ApiResponse<OrganizationUnitsUpdateResponse>>> Update(int id, [FromBody] OrganizationUnitsUpdateRequest request)
    {
        try
        {
            var organizationUnit = await _organizationUnitsService.UpdateAsync(id, request);
            if (organizationUnit is null)
            {
                return NotFound(ApiResponse<OrganizationUnitsUpdateResponse>.FailResponse("Organization unit not found", 404));
            }

            _logger.LogInformation("Updated organization unit '{Code}'.", organizationUnit.Code);
            return Ok(ApiResponse<OrganizationUnitsUpdateResponse>.SuccessResponse(MapToUpdateResponse(organizationUnit), "Organization unit updated successfully", 200));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ApiResponse<OrganizationUnitsUpdateResponse>.FailResponse(ex.Message, 400));
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(ApiResponse<OrganizationUnitsUpdateResponse>.FailResponse(ex.Message, 409));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponse<OrganizationUnitsUpdateResponse>.FailResponse(ex.Message, 404));
        }
    }

    /// <summary>
    /// Từ chối xóa (409) org unit đang được user/role tham chiếu hoặc còn có org unit con.
    /// </summary>
    [HttpDelete("{id:int}")]
    public async Task<ActionResult<ApiResponse>> Delete(int id)
    {
        try
        {
            var deleted = await _organizationUnitsService.DeleteAsync(id);
            if (!deleted)
            {
                return NotFound(ApiResponse.FailResponse("Organization unit not found", 404));
            }

            return Ok(ApiResponse.SuccessResponse("Organization unit deleted successfully", 200));
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(ApiResponse.FailResponse(ex.Message, 409));
        }
    }

    private static OrganizationUnitsGetAllResponse MapToGetAllResponse(OrganizationUnits organizationUnit) => new()
    {
        Id = organizationUnit.Id,
        Code = organizationUnit.Code,
        Name = organizationUnit.Name,
        ParentId = organizationUnit.ParentId,
        Path = organizationUnit.Path,
        Level = organizationUnit.Level,
        SortOrder = organizationUnit.SortOrder,
        IsActive = organizationUnit.IsActive,
        CreatedAt = organizationUnit.CreatedAt,
        CreatedBy = organizationUnit.CreatedBy,
        UpdatedAt = organizationUnit.UpdatedAt,
        UpdatedBy = organizationUnit.UpdatedBy
    };

    private static OrganizationUnitsGetByIdResponse MapToGetByIdResponse(OrganizationUnits organizationUnit) => new()
    {
        Id = organizationUnit.Id,
        Code = organizationUnit.Code,
        Name = organizationUnit.Name,
        ParentId = organizationUnit.ParentId,
        Path = organizationUnit.Path,
        Level = organizationUnit.Level,
        SortOrder = organizationUnit.SortOrder,
        IsActive = organizationUnit.IsActive,
        CreatedAt = organizationUnit.CreatedAt,
        CreatedBy = organizationUnit.CreatedBy,
        UpdatedAt = organizationUnit.UpdatedAt,
        UpdatedBy = organizationUnit.UpdatedBy
    };

    private static OrganizationUnitsCreateResponse MapToCreateResponse(OrganizationUnits organizationUnit) => new()
    {
        Id = organizationUnit.Id,
        Code = organizationUnit.Code,
        Name = organizationUnit.Name,
        ParentId = organizationUnit.ParentId,
        Path = organizationUnit.Path,
        Level = organizationUnit.Level,
        SortOrder = organizationUnit.SortOrder,
        IsActive = organizationUnit.IsActive,
        CreatedAt = organizationUnit.CreatedAt,
        CreatedBy = organizationUnit.CreatedBy,
        UpdatedAt = organizationUnit.UpdatedAt,
        UpdatedBy = organizationUnit.UpdatedBy
    };

    private static OrganizationUnitsUpdateResponse MapToUpdateResponse(OrganizationUnits organizationUnit) => new()
    {
        Id = organizationUnit.Id,
        Code = organizationUnit.Code,
        Name = organizationUnit.Name,
        ParentId = organizationUnit.ParentId,
        Path = organizationUnit.Path,
        Level = organizationUnit.Level,
        SortOrder = organizationUnit.SortOrder,
        IsActive = organizationUnit.IsActive,
        CreatedAt = organizationUnit.CreatedAt,
        CreatedBy = organizationUnit.CreatedBy,
        UpdatedAt = organizationUnit.UpdatedAt,
        UpdatedBy = organizationUnit.UpdatedBy
    };
}
