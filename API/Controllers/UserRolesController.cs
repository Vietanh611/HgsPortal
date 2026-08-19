using API.Authorization;
using Core.Interfaces.Identity;
using Domain.Entities.Identity;
using Hgs.Share.Exceptions;
using Hgs.Share.Requests.UserRoles;
using Hgs.Share.Responses.ApiResponses;
using Hgs.Share.Responses.UserRoles;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace API.Controllers;

[ApiController]
[Route("api/[controller]")]
[MenuPermission("USERS")]
public class UserRolesController : ControllerBase
{
    private readonly IUserRoleService _userRoleService;
    private readonly ILogger<UserRolesController> _logger;

    public UserRolesController(IUserRoleService userRoleService, ILogger<UserRolesController> logger)
    {
        _userRoleService = userRoleService;
        _logger = logger;
    }

    private int GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrWhiteSpace(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
        {
            throw new UnauthorizedException("Unable to determine current user");
        }
        return userId;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<IEnumerable<UserRolesGetAllResponse>>>> GetAll(CancellationToken cancellationToken)
    {
        var userRoles = await _userRoleService.GetAllAsync(cancellationToken);
        var response = userRoles.Select(MapToGetAllResponse).ToList();
        return Ok(ApiResponse<IEnumerable<UserRolesGetAllResponse>>.SuccessResponse(response));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<UserRolesGetByIdResponse>>> GetById(int id, CancellationToken cancellationToken)
    {
        var userRole = await _userRoleService.GetByIdAsync(id, cancellationToken);
        if (userRole is null)
        {
            throw new NotFoundException($"User role assignment with ID {id} not found");
        }
        return Ok(ApiResponse<UserRolesGetByIdResponse>.SuccessResponse(MapToGetByIdResponse(userRole)));
    }

    [HttpGet("by-user/{userId}")]
    public async Task<ActionResult<ApiResponse<IEnumerable<UserRolesGetAllResponse>>>> GetByUserId(int userId, CancellationToken cancellationToken)
    {
        var userRoles = await _userRoleService.GetByUserIdAsync(userId, cancellationToken);
        var response = userRoles.Select(MapToGetAllResponse).ToList();
        return Ok(ApiResponse<IEnumerable<UserRolesGetAllResponse>>.SuccessResponse(response));
    }

    [HttpGet("by-role/{roleId}")]
    public async Task<ActionResult<ApiResponse<IEnumerable<UserRolesGetAllResponse>>>> GetByRoleId(int roleId, CancellationToken cancellationToken)
    {
        var userRoles = await _userRoleService.GetByRoleIdAsync(roleId, cancellationToken);
        var response = userRoles.Select(MapToGetAllResponse).ToList();
        return Ok(ApiResponse<IEnumerable<UserRolesGetAllResponse>>.SuccessResponse(response));
    }

/// <summary>
    /// Gán role cho user; kiểm tra user nằm trong org-scope và role thuộc loại gán được
    /// (active, non-system, org trong phạm vi) — vi phạm → 403.
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<ApiResponse<UserRolesCreateResponse>>> Create([FromBody] UserRolesCreateRequest request, CancellationToken cancellationToken)
    {
        var currentUserId = GetCurrentUserId();
        var userRole = await _userRoleService.CreateAsync(request, currentUserId, cancellationToken);
        _logger.LogInformation("User {CurrentUserId} assigned role {RoleId} to user {UserId}", currentUserId, request.RoleId, request.UserId);
        return Ok(ApiResponse<UserRolesCreateResponse>.SuccessResponse(MapToCreateResponse(userRole)));
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<ApiResponse<UserRolesUpdateResponse>>> Update(int id, [FromBody] UserRolesUpdateRequest request, CancellationToken cancellationToken)
    {
        var userRole = await _userRoleService.UpdateAsync(id, request, cancellationToken);
        if (userRole is null)
        {
            throw new NotFoundException($"User role assignment with ID {id} not found");
        }
        _logger.LogInformation("Updated user role assignment {Id}", id);
        return Ok(ApiResponse<UserRolesUpdateResponse>.SuccessResponse(MapToUpdateResponse(userRole)));
    }

/// <summary>
    /// Gỡ gán role; từ chối khi đây là role cuối cùng của user để đảm bảo user luôn có ít nhất một role.
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<ActionResult<ApiResponse<bool>>> Delete(int id, CancellationToken cancellationToken)
    {
        var result = await _userRoleService.DeleteAsync(id, cancellationToken);
        if (!result)
        {
            throw new NotFoundException($"User role assignment with ID {id} not found");
        }
        _logger.LogInformation("Deleted user role assignment {Id}", id);
        return Ok(ApiResponse<bool>.SuccessResponse(true));
    }

/// <summary>
    /// Gán nhiều role cho một user; mỗi role phải thuộc loại gán được và user phải nằm trong
    /// org-scope — ngoài phạm vi → 403.
    /// </summary>
    [HttpPost("assign-multiple")]
    public async Task<ActionResult<ApiResponse<bool>>> AssignMultipleRoles([FromBody] UserRolesAssignMultipleRequest request, CancellationToken cancellationToken)
    {
        var currentUserId = GetCurrentUserId();
        await _userRoleService.AssignMultipleRolesAsync(request.UserId, request.RoleIds, currentUserId, request.ExpiredAt, cancellationToken);
        _logger.LogInformation("User {CurrentUserId} assigned multiple roles to user {UserId}", currentUserId, request.UserId);
        return Ok(ApiResponse<bool>.SuccessResponse(true, "Roles assigned successfully"));
    }

/// <summary>
    /// Gỡ nhiều role khỏi user; từ chối nếu việc gỡ khiến user không còn role nào;
    /// user ngoài org-scope → 403.
    /// </summary>
    [HttpPost("remove-multiple")]
    public async Task<ActionResult<ApiResponse<bool>>> RemoveMultipleRoles([FromBody] UserRolesAssignMultipleRequest request, CancellationToken cancellationToken)
    {
        await _userRoleService.RemoveMultipleRolesAsync(request.UserId, request.RoleIds, cancellationToken);
        _logger.LogInformation("Removed multiple roles from user {UserId}", request.UserId);
        return Ok(ApiResponse<bool>.SuccessResponse(true, "Roles removed successfully"));
    }

    private static UserRolesGetAllResponse MapToGetAllResponse(UserRoles userRole) => new()
    {
        Id = userRole.Id,
        UserId = userRole.UserId,
        Username = userRole.User?.Username ?? string.Empty,
        UserFullName = userRole.User?.FullName ?? string.Empty,
        RoleId = userRole.RoleId,
        RoleCode = userRole.Role?.Code ?? string.Empty,
        RoleName = userRole.Role?.Name ?? string.Empty,
        AssignedAt = userRole.AssignedAt,
        AssignedBy = userRole.AssignedBy
    };

    private static UserRolesGetByIdResponse MapToGetByIdResponse(UserRoles userRole) => new()
    {
        Id = userRole.Id,
        UserId = userRole.UserId,
        Username = userRole.User?.Username ?? string.Empty,
        UserFullName = userRole.User?.FullName ?? string.Empty,
        RoleId = userRole.RoleId,
        RoleCode = userRole.Role?.Code ?? string.Empty,
        RoleName = userRole.Role?.Name ?? string.Empty,
        AssignedAt = userRole.AssignedAt,
        AssignedBy = userRole.AssignedBy
    };

    private static UserRolesCreateResponse MapToCreateResponse(UserRoles userRole) => new()
    {
        Id = userRole.Id,
        UserId = userRole.UserId,
        RoleId = userRole.RoleId,
        AssignedAt = userRole.AssignedAt
    };

    private static UserRolesUpdateResponse MapToUpdateResponse(UserRoles userRole) => new()
    {
        Id = userRole.Id,
        UserId = userRole.UserId,
        RoleId = userRole.RoleId,
        AssignedAt = userRole.AssignedAt
    };
}
