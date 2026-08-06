using Core.Interfaces;
using Domain.Entities.System;
using Hgs.Share.Requests.Menus;
using Hgs.Share.Responses.ApiResponses;
using Hgs.Share.Responses.Menus;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class MenusController : ControllerBase
{
    private readonly IMenuService _menuService;
    private readonly ILogger<MenusController> _logger;

    public MenusController(IMenuService menuService, ILogger<MenusController> logger)
    {
        _menuService = menuService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<IEnumerable<MenusGetAllResponse>>>> GetAll()
    {
        var menus = await _menuService.GetAllAsync();
        var response = menus.Select(MapToGetAllResponse).ToList();

        return Ok(ApiResponse<IEnumerable<MenusGetAllResponse>>.SuccessResponse(response, "Menus retrieved successfully", 200));
    }

    [HttpGet("all")]
    public async Task<ActionResult<ApiResponse<IEnumerable<MenusGetAllResponse>>>> GetAllFlat()
    {
        var menus = await _menuService.GetAllFlatAsync();
        var response = menus.Select(MapToGetAllResponseFlat).ToList();

        return Ok(ApiResponse<IEnumerable<MenusGetAllResponse>>.SuccessResponse(response, "Menus retrieved successfully", 200));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ApiResponse<MenusGetByIdResponse>>> GetById(int id)
    {
        var menu = await _menuService.GetByIdAsync(id);
        if (menu is null)
        {
            return NotFound(ApiResponse<MenusGetByIdResponse>.FailResponse("Menu not found", 404));
        }

        return Ok(ApiResponse<MenusGetByIdResponse>.SuccessResponse(MapToGetByIdResponse(menu), "Menu retrieved successfully", 200));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<MenusCreateResponse>>> Create([FromBody] MenusCreateRequest request)
    {
        try
        {
            var menu = await _menuService.CreateAsync(request);
            _logger.LogInformation("Created menu '{Code}'.", menu.Code);
            return Ok(ApiResponse<MenusCreateResponse>.SuccessResponse(MapToCreateResponse(menu), "Menu created successfully", 201));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ApiResponse<MenusCreateResponse>.FailResponse(ex.Message, 400));
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(ApiResponse<MenusCreateResponse>.FailResponse(ex.Message, 409));
        }
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<ApiResponse<MenusUpdateResponse>>> Update(int id, [FromBody] MenusUpdateRequest request)
    {
        try
        {
            var menu = await _menuService.UpdateAsync(id, request);
            if (menu is null)
            {
                return NotFound(ApiResponse<MenusUpdateResponse>.FailResponse("Menu not found", 404));
            }

            _logger.LogInformation("Updated menu '{Code}'.", menu.Code);
            return Ok(ApiResponse<MenusUpdateResponse>.SuccessResponse(MapToUpdateResponse(menu), "Menu updated successfully", 200));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ApiResponse<MenusUpdateResponse>.FailResponse(ex.Message, 400));
        }
    }

    [HttpDelete("{id:int}")]
    public async Task<ActionResult<ApiResponse>> Delete(int id)
    {
        var deleted = await _menuService.DeleteAsync(id);
        if (!deleted)
        {
            return NotFound(ApiResponse.FailResponse("Menu not found", 404));
        }

        _logger.LogInformation("Deleted menu with id '{Id}'.", id);
        return Ok(ApiResponse.SuccessResponse("Menu deleted successfully", 200));
    }

    [HttpGet("user/{userId:int}")]
    public async Task<ActionResult<ApiResponse<IEnumerable<MenusGetByUserIdResponse>>>> GetByUserId(int userId)
    {
        var menus = await _menuService.GetMenusByUserIdAsync(userId);
        return Ok(ApiResponse<IEnumerable<MenusGetByUserIdResponse>>.SuccessResponse(menus, "Menus retrieved successfully for user", 200));
    }

    private static MenusGetAllResponse MapToGetAllResponse(Menus menu) => new()
    {
        Id = menu.Id,
        ParentId = menu.ParentId,
        Code = menu.Code,
        Name = menu.Name,
        Route = menu.Route,
        Component = menu.Component,
        Icon = menu.Icon,
        SortOrder = menu.SortOrder,
        IsVisible = menu.IsVisible,
        IsActive = menu.IsActive,
        CreatedAt = menu.CreatedAt,
        CreatedBy = menu.CreatedBy,
        UpdatedAt = menu.UpdatedAt,
        UpdatedBy = menu.UpdatedBy,
        Children = menu.Children.Select(MapToGetAllResponse).ToList()
    };

    private static MenusGetAllResponse MapToGetAllResponseFlat(Menus menu) => new()
    {
        Id = menu.Id,
        ParentId = menu.ParentId,
        Code = menu.Code,
        Name = menu.Name,
        Route = menu.Route,
        Component = menu.Component,
        Icon = menu.Icon,
        SortOrder = menu.SortOrder,
        IsVisible = menu.IsVisible,
        IsActive = menu.IsActive,
        CreatedAt = menu.CreatedAt,
        CreatedBy = menu.CreatedBy,
        UpdatedAt = menu.UpdatedAt,
        UpdatedBy = menu.UpdatedBy,
        Children = new List<MenusGetAllResponse>()
    };

    private static MenusGetByIdResponse MapToGetByIdResponse(Menus menu) => new()
    {
        Id = menu.Id,
        ParentId = menu.ParentId,
        Code = menu.Code,
        Name = menu.Name,
        Route = menu.Route,
        Component = menu.Component,
        Icon = menu.Icon,
        SortOrder = menu.SortOrder,
        IsVisible = menu.IsVisible,
        IsActive = menu.IsActive,
        CreatedAt = menu.CreatedAt,
        CreatedBy = menu.CreatedBy,
        UpdatedAt = menu.UpdatedAt,
        UpdatedBy = menu.UpdatedBy
    };

    private static MenusCreateResponse MapToCreateResponse(Menus menu) => new()
    {
        Id = menu.Id,
        ParentId = menu.ParentId,
        Code = menu.Code,
        Name = menu.Name,
        Route = menu.Route,
        Component = menu.Component,
        Icon = menu.Icon,
        SortOrder = menu.SortOrder,
        IsVisible = menu.IsVisible,
        IsActive = menu.IsActive,
        CreatedAt = menu.CreatedAt,
        CreatedBy = menu.CreatedBy,
        UpdatedAt = menu.UpdatedAt,
        UpdatedBy = menu.UpdatedBy
    };

    private static MenusUpdateResponse MapToUpdateResponse(Menus menu) => new()
    {
        Id = menu.Id,
        ParentId = menu.ParentId,
        Code = menu.Code,
        Name = menu.Name,
        Route = menu.Route,
        Component = menu.Component,
        Icon = menu.Icon,
        SortOrder = menu.SortOrder,
        IsVisible = menu.IsVisible,
        IsActive = menu.IsActive,
        CreatedAt = menu.CreatedAt,
        CreatedBy = menu.CreatedBy,
        UpdatedAt = menu.UpdatedAt,
        UpdatedBy = menu.UpdatedBy
    };
}
