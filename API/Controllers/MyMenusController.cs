using Core.Interfaces;
using Hgs.Share.Responses.ApiResponses;
using Hgs.Share.Responses.Menus;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace API.Controllers;

[ApiController]
[Route("api/my-menus")]
[Authorize]
public class MyMenusController : ControllerBase
{
    private readonly IMenuService _menuService;

    public MyMenusController(IMenuService menuService)
    {
        _menuService = menuService;
    }

    private int GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrWhiteSpace(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
        {
            return 0;
        }
        return userId;
    }

    /// <summary>
    /// Cây menu sidebar của user đang đăng nhập; user có role SUPER_ADMIN nhận toàn bộ menu hệ thống.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<ApiResponse<IEnumerable<MenusGetByUserIdResponse>>>> GetMyMenus(CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId <= 0)
        {
            return Unauthorized(ApiResponse.FailResponse("User not authenticated", 401));
        }

        var menus = await _menuService.GetMenusByUserIdAsync(userId, cancellationToken);
        return Ok(ApiResponse<IEnumerable<MenusGetByUserIdResponse>>.SuccessResponse(menus, "Menus retrieved successfully", 200));
    }
}