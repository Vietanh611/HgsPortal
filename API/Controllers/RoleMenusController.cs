using API.Authorization;
using Core.Interfaces.Identity;
using Domain.Entities.System;
using Hgs.Share.Exceptions;
using Hgs.Share.Requests.RoleMenus;
using Hgs.Share.Responses.ApiResponses;
using Hgs.Share.Responses.RoleMenus;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace API.Controllers;

/// <summary>
/// Quản lý gán menu cho role — nền tảng của phân quyền menu (RBAC): mọi user mang role sẽ kế
/// thừa các menu này, nên mỗi thay đổi ở đây sẽ xóa toàn bộ cache menu để quyền mới phản ánh
/// ngay (xem IRoleMenuService). Chỉ user có menu USERS mới được thao tác.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[MenuPermission("USERS")]
public class RoleMenusController : ControllerBase
{
    private readonly IRoleMenuService _roleMenuService;
    private readonly ILogger<RoleMenusController> _logger;

    public RoleMenusController(IRoleMenuService roleMenuService, ILogger<RoleMenusController> logger)
    {
        _roleMenuService = roleMenuService;
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
    public async Task<ActionResult<ApiResponse<IEnumerable<RoleMenusGetAllResponse>>>> GetAll(CancellationToken cancellationToken)
    {
        var roleMenus = await _roleMenuService.GetAllAsync(cancellationToken);
        var response = roleMenus.Select(MapToGetAllResponse).ToList();
        return Ok(ApiResponse<IEnumerable<RoleMenusGetAllResponse>>.SuccessResponse(response));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<RoleMenusGetByIdResponse>>> GetById(int id, CancellationToken cancellationToken)
    {
        var roleMenu = await _roleMenuService.GetByIdAsync(id, cancellationToken);
        if (roleMenu is null)
        {
            throw new NotFoundException($"Role menu assignment with ID {id} not found");
        }
        return Ok(ApiResponse<RoleMenusGetByIdResponse>.SuccessResponse(MapToGetByIdResponse(roleMenu)));
    }

    [HttpGet("by-role/{roleId}")]
    public async Task<ActionResult<ApiResponse<IEnumerable<RoleMenusGetAllResponse>>>> GetByRoleId(int roleId, CancellationToken cancellationToken)
    {
        var roleMenus = await _roleMenuService.GetByRoleIdAsync(roleId, cancellationToken);
        var response = roleMenus.Select(MapToGetAllResponse).ToList();
        return Ok(ApiResponse<IEnumerable<RoleMenusGetAllResponse>>.SuccessResponse(response));
    }

    [HttpGet("by-menu/{menuId}")]
    public async Task<ActionResult<ApiResponse<IEnumerable<RoleMenusGetAllResponse>>>> GetByMenuId(int menuId, CancellationToken cancellationToken)
    {
        var roleMenus = await _roleMenuService.GetByMenuIdAsync(menuId, cancellationToken);
        var response = roleMenus.Select(MapToGetAllResponse).ToList();
        return Ok(ApiResponse<IEnumerable<RoleMenusGetAllResponse>>.SuccessResponse(response));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<RoleMenusCreateResponse>>> Create([FromBody] RoleMenusCreateRequest request, CancellationToken cancellationToken)
    {
        var currentUserId = GetCurrentUserId();
        var roleMenu = await _roleMenuService.CreateAsync(request, currentUserId, cancellationToken);
        _logger.LogInformation("User {CurrentUserId} assigned menu {MenuId} to role {RoleId}", currentUserId, request.MenuId, request.RoleId);
        return Ok(ApiResponse<RoleMenusCreateResponse>.SuccessResponse(MapToCreateResponse(roleMenu)));
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult<ApiResponse<bool>>> Delete(int id, CancellationToken cancellationToken)
    {
        var result = await _roleMenuService.DeleteAsync(id, cancellationToken);
        if (!result)
        {
            throw new NotFoundException($"Role menu assignment with ID {id} not found");
        }
        _logger.LogInformation("Deleted role menu assignment {Id}", id);
        return Ok(ApiResponse<bool>.SuccessResponse(true));
    }

    [HttpPost("assign-multiple")]
    public async Task<ActionResult<ApiResponse<bool>>> AssignMultipleMenus([FromBody] RoleMenusAssignMultipleRequest request, CancellationToken cancellationToken)
    {
        var currentUserId = GetCurrentUserId();
        await _roleMenuService.AssignMultipleMenusAsync(request.RoleId, request.MenuIds, currentUserId, cancellationToken);
        _logger.LogInformation("User {CurrentUserId} assigned multiple menus to role {RoleId}", currentUserId, request.RoleId);
        return Ok(ApiResponse<bool>.SuccessResponse(true, "Menus assigned successfully"));
    }

    [HttpPost("remove-multiple")]
    public async Task<ActionResult<ApiResponse<bool>>> RemoveMultipleMenus([FromBody] RoleMenusAssignMultipleRequest request, CancellationToken cancellationToken)
    {
        await _roleMenuService.RemoveMultipleMenusAsync(request.RoleId, request.MenuIds, cancellationToken);
        _logger.LogInformation("Removed multiple menus from role {RoleId}", request.RoleId);
        return Ok(ApiResponse<bool>.SuccessResponse(true, "Menus removed successfully"));
    }

    private static RoleMenusGetAllResponse MapToGetAllResponse(RoleMenus roleMenu) => new()
    {
        Id = roleMenu.Id,
        RoleId = roleMenu.RoleId,
        RoleCode = roleMenu.Role?.Code ?? string.Empty,
        RoleName = roleMenu.Role?.Name ?? string.Empty,
        MenuId = roleMenu.MenuId,
        MenuCode = roleMenu.Menu?.Code ?? string.Empty,
        MenuName = roleMenu.Menu?.Name ?? string.Empty,
        CreatedAt = roleMenu.CreatedAt,
        CreatedBy = roleMenu.CreatedBy
    };

    private static RoleMenusGetByIdResponse MapToGetByIdResponse(RoleMenus roleMenu) => new()
    {
        Id = roleMenu.Id,
        RoleId = roleMenu.RoleId,
        RoleCode = roleMenu.Role?.Code ?? string.Empty,
        RoleName = roleMenu.Role?.Name ?? string.Empty,
        MenuId = roleMenu.MenuId,
        MenuCode = roleMenu.Menu?.Code ?? string.Empty,
        MenuName = roleMenu.Menu?.Name ?? string.Empty,
        CreatedAt = roleMenu.CreatedAt,
        CreatedBy = roleMenu.CreatedBy
    };

    private static RoleMenusCreateResponse MapToCreateResponse(RoleMenus roleMenu) => new()
    {
        Id = roleMenu.Id,
        RoleId = roleMenu.RoleId,
        MenuId = roleMenu.MenuId,
        CreatedAt = roleMenu.CreatedAt,
    };
}
