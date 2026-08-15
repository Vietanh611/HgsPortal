using API.Authorization;
using Core.Interfaces;
using Domain.Entities.Identity;
using Hgs.Share.Exceptions;
using Hgs.Share.Requests.Roles;
using Hgs.Share.Responses.ApiResponses;
using Hgs.Share.Responses.Roles;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ApiController]
[Route("api/[controller]")]
[MenuPermission("ROLES")]
public class RolesController : ControllerBase
{
    private readonly IRolesService _rolesService;
    private readonly ILogger<RolesController> _logger;

    public RolesController(IRolesService rolesService, ILogger<RolesController> logger)
    {
        _rolesService = rolesService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<IEnumerable<RolesGetAllResponse>>>> GetAll(CancellationToken cancellationToken)
    {
        var roles = await _rolesService.GetAllAsync(cancellationToken);
        var response = roles.Select(MapToGetAllResponse).ToList();
        return Ok(ApiResponse<IEnumerable<RolesGetAllResponse>>.SuccessResponse(response));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<RolesGetByIdResponse>>> GetById(int id, CancellationToken cancellationToken)
    {
        var role = await _rolesService.GetByIdAsync(id, cancellationToken);
        if (role is null)
        {
            throw new NotFoundException($"Role with ID {id} not found");
        }
        return Ok(ApiResponse<RolesGetByIdResponse>.SuccessResponse(MapToGetByIdResponse(role)));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<RolesCreateResponse>>> Create([FromBody] RolesCreateRequest request, CancellationToken cancellationToken)
    {
        var role = new Roles
        {
            Code = request.Code,
            Name = request.Name,
            Description = request.Description,
            OrganizationUnitId = request.OrganizationUnitId,
            DataScope = request.DataScope,
            IsSystemRole = request.IsSystemRole,
            IsActive = request.IsActive,
            CreatedAt = DateTime.UtcNow
        };

        var createdRole = await _rolesService.CreateAsync(role, cancellationToken);
        _logger.LogInformation("Created role '{Code}'.", createdRole.Code);
        return Ok(ApiResponse<RolesCreateResponse>.SuccessResponse(MapToCreateResponse(createdRole)));
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<ApiResponse<RolesUpdateResponse>>> Update(int id, [FromBody] RolesUpdateRequest request, CancellationToken cancellationToken)
    {
        var role = new Roles
        {
            Code = request.Code,
            Name = request.Name,
            Description = request.Description,
            OrganizationUnitId = request.OrganizationUnitId,
            DataScope = request.DataScope,
            IsSystemRole = request.IsSystemRole,
            IsActive = request.IsActive
        };

        var updatedRole = await _rolesService.UpdateAsync(id, role, cancellationToken);
        if (updatedRole is null)
        {
            throw new NotFoundException($"Role with ID {id} not found");
        }

        _logger.LogInformation("Updated role '{Code}'.", updatedRole.Code);
        return Ok(ApiResponse<RolesUpdateResponse>.SuccessResponse(MapToUpdateResponse(updatedRole)));
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult<ApiResponse<bool>>> Delete(int id, CancellationToken cancellationToken)
    {
        var result = await _rolesService.DeleteAsync(id, cancellationToken);
        if (!result)
        {
            throw new NotFoundException($"Role with ID {id} not found");
        }

        _logger.LogInformation("Deleted role with id '{Id}'.", id);
        return Ok(ApiResponse<bool>.SuccessResponse(true));
    }

    private static RolesGetAllResponse MapToGetAllResponse(Roles role) => new()
    {
        Id = role.Id,
        Code = role.Code,
        Name = role.Name,
        Description = role.Description,
        OrganizationUnitId = role.OrganizationUnitId,
        OrganizationUnitName = role.OrganizationUnit?.Name,
        DataScope = role.DataScope,
        IsSystemRole = role.IsSystemRole,
        IsActive = role.IsActive,
        CreatedAt = role.CreatedAt,
        CreatedBy = role.CreatedBy
    };

    private static RolesGetByIdResponse MapToGetByIdResponse(Roles role) => new()
    {
        Id = role.Id,
        Code = role.Code,
        Name = role.Name,
        Description = role.Description,
        OrganizationUnitId = role.OrganizationUnitId,
        OrganizationUnitName = role.OrganizationUnit?.Name,
        DataScope = role.DataScope,
        IsSystemRole = role.IsSystemRole,
        IsActive = role.IsActive,
        CreatedAt = role.CreatedAt,
        CreatedBy = role.CreatedBy
    };

    private static RolesCreateResponse MapToCreateResponse(Roles role) => new()
    {
        Id = role.Id,
        Code = role.Code,
        Name = role.Name,
        CreatedAt = role.CreatedAt
    };

    private static RolesUpdateResponse MapToUpdateResponse(Roles role) => new()
    {
        Id = role.Id,
        Code = role.Code,
        Name = role.Name
    };
}
