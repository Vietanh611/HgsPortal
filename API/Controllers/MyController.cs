using API.Extensions;
using Core.Interfaces.Identity;
using Domain.Entities.Identity;
using Hgs.Share.Responses.ApiResponses;
using Hgs.Share.Responses.Menus;
using Hgs.Share.Responses.Roles;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

/// <summary>
/// Dữ liệu phân quyền của user đang đăng nhập (cây menu + role) — nguồn hiển thị cho sidebar
/// và các modal gán quyền; gộp từ MyMenusController/MyRolesController để nhóm đúng một chủ thể "my".
/// </summary>
[ApiController]
[Route("api/my")]
[Authorize]
public class MyController : ControllerBase
{
    private readonly IMenuService _menuService;
    private readonly IOrgScopeService _orgScope;

    public MyController(IMenuService menuService, IOrgScopeService orgScope)
    {
        _menuService = menuService;
        _orgScope = orgScope;
    }

    /// <summary>
    /// Cây menu sidebar của user đang đăng nhập; user có role SUPER_ADMIN nhận toàn bộ menu hệ thống.
    /// </summary>
    [HttpGet("menus")]
    public async Task<ActionResult<ApiResponse<IEnumerable<MenusGetByUserIdResponse>>>> GetMyMenus(CancellationToken cancellationToken)
    {
        if (!User.TryGetCurrentUserId(out var userId))
        {
            return Unauthorized(ApiResponse.FailResponse("User not authenticated", 401));
        }

        var menus = await _menuService.GetMenusByUserIdAsync(userId, cancellationToken);
        return Ok(ApiResponse<IEnumerable<MenusGetByUserIdResponse>>.SuccessResponse(menus, "Menus retrieved successfully", 200));
    }

    /// <summary>
    /// Các role cho modal gán vai trò (Quản lý tài khoản) — cùng nguồn với Quản lý phân quyền:
    /// SUPER_ADMIN nhận toàn bộ role đang hoạt động không phải role hệ thống;
    /// admin thường chỉ thấy và gán được đúng những role mình đang giữ, không thể vượt quyền cấp role ngoài phạm vi của mình.
    /// </summary>
    [HttpGet("roles")]
    public async Task<ActionResult<ApiResponse<IEnumerable<RolesGetAllResponse>>>> GetMyRoles(CancellationToken cancellationToken)
    {
        if (!User.TryGetCurrentUserId(out _))
        {
            return Unauthorized(ApiResponse.FailResponse("User not authenticated", 401));
        }

        var roles = await _orgScope.GetAssignableRolesAsync(cancellationToken);
        var result = roles
            .Where(r => r is not null)
            .Select(r => MapToGetAllResponse(r))
            .ToList();
        return Ok(ApiResponse<IEnumerable<RolesGetAllResponse>>.SuccessResponse(result, "Roles retrieved successfully", 200));
    }

    private static RolesGetAllResponse MapToGetAllResponse(Roles role) => new()
    {
        Id = role.Id,
        Code = role.Code,
        Name = role.Name,
        Description = role.Description,
        OrganizationUnitId = role.OrganizationUnitId,
        DataScope = role.DataScope,
        IsSystemRole = role.IsSystemRole,
        IsActive = role.IsActive,
        CreatedAt = role.CreatedAt,
        CreatedBy = role.CreatedBy
    };
}