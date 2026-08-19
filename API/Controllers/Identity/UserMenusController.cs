using API.Authorization;
using Core.Interfaces.Identity;
using Hgs.Share.Dtos;
using Hgs.Share.Requests.UserMenus;
using Hgs.Share.Responses.ApiResponses;
using Hgs.Share.Responses.Menus;
using Hgs.Share.Responses.UserMenus;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace API.Controllers.Identity;

[ApiController]
[Route("api/[controller]")]
[MenuPermission("USERS")]
public class UserMenusController : ControllerBase
{
    private readonly IUserMenuService _userMenuService;
    private readonly IMenuService _menuService;
    private readonly ILogger<UserMenusController> _logger;

    public UserMenusController(IUserMenuService userMenuService, IMenuService menuService, ILogger<UserMenusController> logger)
    {
        _userMenuService = userMenuService;
        _menuService = menuService;
        _logger = logger;
    }

    private int GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrWhiteSpace(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
        {
            return 0; // Return 0 if not authenticated, or throw exception
        }
        return userId;
    }

    [HttpGet]
    public async Task<ActionResult> GetAll(CancellationToken cancellationToken)
    {
        var userMenus = await _userMenuService.GetAllAsync(cancellationToken);
        return Ok(ApiResponse<IEnumerable<UserMenuDto>>.SuccessResponse(userMenus, "User menus retrieved successfully", 200));
    }

    [HttpGet("user/{userId:int}")]
    public async Task<ActionResult<ApiResponse<IEnumerable<MenusGetByUserIdResponse>>>> GetByUserId(int userId, CancellationToken cancellationToken)
    {
        var menus = await _menuService.GetMenusByUserIdAsync(userId, cancellationToken);
        return Ok(ApiResponse<IEnumerable<MenusGetByUserIdResponse>>.SuccessResponse(menus, "Menus retrieved successfully for user", 200));
    }

    /// <summary>
    /// Chỉ trả về các menu gán trực tiếp cho user (bảng UserMenus), không bao gồm menu kế thừa từ role.
    /// </summary>
    [HttpGet("user/{userId:int}/menu-ids")]
    public async Task<ActionResult<ApiResponse<IEnumerable<int>>>> GetMenuIdsByUserId(int userId, CancellationToken cancellationToken)
    {
        var menuIds = await _userMenuService.GetMenuIdsByUserIdAsync(userId, cancellationToken);
        return Ok(ApiResponse<IEnumerable<int>>.SuccessResponse(menuIds, "Menu IDs retrieved successfully for user", 200));
    }

    /// <summary>
    /// Trả về tách biệt menu kế thừa từ role (RoleMenuIds) và menu gán trực tiếp (UserMenuIds)
    /// để UI phân biệt được nguồn gốc từng menu.
    /// </summary>
    [HttpGet("user/{userId:int}/details")]
    public async Task<ActionResult<ApiResponse<UserMenuAssignmentDetailsResponse>>> GetAssignmentDetailsByUserId(int userId, CancellationToken cancellationToken)
    {
        var details = await _userMenuService.GetAssignmentDetailsByUserIdAsync(userId, cancellationToken);
        return Ok(ApiResponse<UserMenuAssignmentDetailsResponse>.SuccessResponse(details, "User menu assignment details retrieved successfully", 200));
    }

    /// <summary>
    /// Gán menu trực tiếp cho user; user đích phải nằm trong org-scope → ngoài phạm vi 403.
    /// </summary>
    [HttpPost]
    public async Task<ActionResult> Create([FromBody] UserMenusCreateRequest request, CancellationToken cancellationToken)
    {
        var currentUserId = GetCurrentUserId();
        var userMenu = await _userMenuService.CreateAsync(request, currentUserId, cancellationToken);
        _logger.LogInformation("User {CurrentUserId} assigned menu {MenuId} to user {UserId}", currentUserId, request.MenuId, request.UserId);
        return Ok(ApiResponse.SuccessResponse("Menu assigned successfully", 201));
    }

    [HttpDelete("{id:int}")]
    public async Task<ActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var deleted = await _userMenuService.DeleteAsync(id, cancellationToken);
        if (!deleted)
        {
            return NotFound(ApiResponse.FailResponse("User menu not found", 404));
        }
        _logger.LogInformation("Deleted user menu assignment {Id}", id);
        return Ok(ApiResponse.SuccessResponse("User menu deleted successfully", 200));
    }

    /// <summary>
    /// Gán nhiều menu cho user; user đích phải nằm trong org-scope → ngoài phạm vi 403.
    /// </summary>
    [HttpPost("assign-multiple")]
    public async Task<ActionResult> AssignMultipleMenus([FromBody] UserMenusAssignMultipleRequest request, CancellationToken cancellationToken)
    {
        var currentUserId = GetCurrentUserId();
        await _userMenuService.AssignMultipleMenusAsync(request.UserId, request.MenuIds, currentUserId, cancellationToken);
        _logger.LogInformation("User {CurrentUserId} assigned multiple menus to user {UserId}", currentUserId, request.UserId);
        return Ok(ApiResponse.SuccessResponse("Menus assigned successfully", 200));
    }

    /// <summary>
    /// Gỡ nhiều menu khỏi user; user đích phải nằm trong org-scope → ngoài phạm vi 403.
    /// </summary>
    [HttpPost("remove-multiple")]
    public async Task<ActionResult> RemoveMultipleMenus([FromBody] UserMenusAssignMultipleRequest request, CancellationToken cancellationToken)
    {
        await _userMenuService.RemoveMultipleMenusAsync(request.UserId, request.MenuIds, cancellationToken);
        _logger.LogInformation("Removed multiple menus from user {UserId}", request.UserId);
        return Ok(ApiResponse.SuccessResponse("Menus removed successfully", 200));
    }
}
