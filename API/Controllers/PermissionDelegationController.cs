using API.Authorization;
using Core.Interfaces;
using Hgs.Share.Requests.PermissionDelegation;
using Hgs.Share.Responses.ApiResponses;
using Hgs.Share.Responses.PermissionDelegation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ApiController]
[Route("api/[controller]")]
[MenuPermission("PERMISSIONDELEGATION")]
public class PermissionDelegationController : ControllerBase
{
    private readonly IPermissionDelegationService _permissionDelegationService;
    private readonly ILogger<PermissionDelegationController> _logger;

    public PermissionDelegationController(
        IPermissionDelegationService permissionDelegationService,
        ILogger<PermissionDelegationController> logger)
    {
        _permissionDelegationService = permissionDelegationService;
        _logger = logger;
    }

    /// <summary>
    /// Danh sách user trong org-scope của người gọi (loại trừ chính mình, chỉ user active)
    /// dùng để chọn người nhận ủy quyền.
    /// </summary>
    [HttpGet("manageable-users")]
    public async Task<ActionResult<ApiResponse<IEnumerable<ManageableUserResponse>>>> GetManageableUsers()
    {
        try
        {
            var users = await _permissionDelegationService.GetManageableUsersAsync();
            return Ok(ApiResponse<IEnumerable<ManageableUserResponse>>.SuccessResponse(users, "Manageable users retrieved successfully", 200));
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized access to manageable users");
            return StatusCode(403, ApiResponse<IEnumerable<ManageableUserResponse>>.FailResponse("Bạn không có quyền thực hiện thao tác này", 403));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving manageable users");
            return StatusCode(500, ApiResponse<IEnumerable<ManageableUserResponse>>.FailResponse("Error retrieving manageable users", 500));
        }
    }

    /// <summary>
    /// Chỉ trả về role gán được: active, không phải role hệ thống và org thuộc org-scope của người gọi.
    /// </summary>
    [HttpGet("assignable-roles")]
    public async Task<ActionResult<ApiResponse<IEnumerable<AssignableRoleResponse>>>> GetAssignableRoles()
    {
        try
        {
            var roles = await _permissionDelegationService.GetAssignableRolesAsync();
            return Ok(ApiResponse<IEnumerable<AssignableRoleResponse>>.SuccessResponse(roles, "Assignable roles retrieved successfully", 200));
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized access to assignable roles");
            return StatusCode(403, ApiResponse<IEnumerable<AssignableRoleResponse>>.FailResponse("Bạn không có quyền thực hiện thao tác này", 403));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving assignable roles");
            return StatusCode(500, ApiResponse<IEnumerable<AssignableRoleResponse>>.FailResponse("Error retrieving assignable roles", 500));
        }
    }

    /// <summary>
    /// Gán role cho user được ủy quyền với chuỗi kiểm tra an toàn: có menu PERMISSIONDELEGATION,
    /// không gán cho chính mình, user đích trong org-scope, role gán được, chưa có sẵn role đó.
    /// </summary>
    [HttpPost("assign-role")]
    public async Task<ActionResult<ApiResponse>> AssignRole([FromBody] AssignRoleRequest request)
    {
        try
        {
            await _permissionDelegationService.AssignRoleAsync(request);
            return Ok(ApiResponse.SuccessResponse("Role assigned successfully", 200));
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized access to assign role");
            return StatusCode(403, ApiResponse.FailResponse("Bạn không có quyền thực hiện thao tác này", 403));
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Target user not found for role assignment");
            return NotFound(ApiResponse.FailResponse("Target user not found", 404));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error assigning role");
            return StatusCode(500, ApiResponse.FailResponse("Error assigning role", 500));
        }
    }

    /// <summary>
    /// Gỡ role ủy quyền với chuỗi kiểm tra an toàn tương tự AssignRole (không gỡ của chính mình,
    /// user trong org-scope, role gán được).
    /// </summary>
    [HttpPost("revoke-role")]
    public async Task<ActionResult<ApiResponse>> RevokeRole([FromBody] RevokeRoleRequest request)
    {
        try
        {
            await _permissionDelegationService.RevokeRoleAsync(request);
            return Ok(ApiResponse.SuccessResponse("Role revoked successfully", 200));
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized access to revoke role");
            return StatusCode(403, ApiResponse.FailResponse("Bạn không có quyền thực hiện thao tác này", 403));
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Target user not found for role revocation");
            return NotFound(ApiResponse.FailResponse("Target user not found", 404));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error revoking role");
            return StatusCode(500, ApiResponse.FailResponse("Error revoking role", 500));
        }
    }

    /// <summary>
    /// Trả về quyền hiệu dụng của user gồm Roles và các menu active lấy qua role.
    /// </summary>
    [HttpGet("user/{userId:int}/effective-permissions")]
    public async Task<ActionResult<ApiResponse<UserEffectivePermissionsResponse>>> GetUserEffectivePermissions(int userId)
    {
        try
        {
            var permissions = await _permissionDelegationService.GetUserEffectivePermissionsAsync(userId);
            if (permissions == null)
            {
                return NotFound(ApiResponse<UserEffectivePermissionsResponse>.FailResponse("User not found", 404));
            }

            return Ok(ApiResponse<UserEffectivePermissionsResponse>.SuccessResponse(permissions, "User permissions retrieved successfully", 200));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving user permissions");
            return StatusCode(500, ApiResponse<UserEffectivePermissionsResponse>.FailResponse("Error retrieving user permissions", 500));
        }
    }
}
