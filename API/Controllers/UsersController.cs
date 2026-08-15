using API.Authorization;
using Core.Interfaces;
using Domain.Entities.FlyOps;
using Domain.Entities.Identity;
using Hgs.Share.Requests.Users;
using Hgs.Share.Responses.ApiResponses;
using Hgs.Share.Responses.Users;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace API.Controllers;

[MenuPermission("USERS")]
public class UsersController : BaseApiController
{
    private readonly IUsersService _usersService;
    private readonly ILogger<UsersController> _logger;

    public UsersController(IUsersService usersService, ILogger<UsersController> logger)
    {
        _usersService = usersService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<IEnumerable<UsersGetAllResponse>>>> GetAll()
    {
        var users = await _usersService.GetAllAsync();
        var response = users.Select(MapToGetAllResponse).ToList();

        return Ok(ApiResponse<IEnumerable<UsersGetAllResponse>>.SuccessResponse(response, "Users retrieved successfully", 200));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ApiResponse<UsersGetByIdResponse>>> GetById(int id)
    {
        var user = await _usersService.GetByIdAsync(id);
        if (user is null)
        {
            return NotFound(ApiResponse<UsersGetByIdResponse>.FailResponse("User not found", 404));
        }

        return Ok(ApiResponse<UsersGetByIdResponse>.SuccessResponse(MapToGetByIdResponse(user), "User retrieved successfully", 200));
    }

    [HttpGet("bravo")]
    public async Task<ActionResult<ApiResponse<IEnumerable<NhanVien>>>> GetBravoAll()
    {
        var nhanvien = await _usersService.GetAllBravoNhanVienAsync();

        return Ok(ApiResponse<IEnumerable<NhanVien>>.SuccessResponse(nhanvien, "Users retrieved successfully", 200));
    }

    [HttpGet("me")]
    public async Task<ActionResult<ApiResponse<UsersGetByIdResponse>>> GetCurrentUser()
    {
        if (!CurrentUserId.HasValue)
        {
            return Unauthorized(ApiResponse<UsersGetByIdResponse>.FailResponse("Invalid user token", 401));
        }

        var user = await _usersService.GetByIdAsync(CurrentUserId.Value);
        if (user is null)
        {
            return NotFound(ApiResponse<UsersGetByIdResponse>.FailResponse("User not found", 404));
        }

        return Ok(ApiResponse<UsersGetByIdResponse>.SuccessResponse(MapToGetByIdResponse(user), "User retrieved successfully", 200));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<UsersCreateResponse>>> Create([FromBody] UsersCreateRequest request)
    {
        try
        {
            var user = await _usersService.CreateAsync(request);
            _logger.LogInformation("Created user '{Username}'.", user.Username);
            return Ok(ApiResponse<UsersCreateResponse>.SuccessResponse(MapToCreateResponse(user), "User created successfully", 201));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ApiResponse<UsersCreateResponse>.FailResponse(ex.Message, 400));
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(ApiResponse<UsersCreateResponse>.FailResponse(ex.Message, 409));
        }
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<ApiResponse<UsersUpdateResponse>>> Update(int id, [FromBody] UsersUpdateRequest request)
    {
        try
        {
            var user = await _usersService.UpdateAsync(id, request);
            if (user is null)
            {
                return NotFound(ApiResponse<UsersUpdateResponse>.FailResponse("User not found", 404));
            }

            _logger.LogInformation("Updated user '{Username}'.", user.Username);
            return Ok(ApiResponse<UsersUpdateResponse>.SuccessResponse(MapToUpdateResponse(user), "User updated successfully", 200));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ApiResponse<UsersUpdateResponse>.FailResponse(ex.Message, 400));
        }
    }

    [HttpDelete("{id:int}")]
    public async Task<ActionResult<ApiResponse>> Delete(int id)
    {
        var deleted = await _usersService.DeleteAsync(id);
        if (!deleted)
        {
            return NotFound(ApiResponse.FailResponse("User not found", 404));
        }

        _logger.LogInformation("Deleted user with id '{Id}'.", id);
        return Ok(ApiResponse.SuccessResponse("User deleted successfully", 200));
    }

    [HttpPut("{id:int}/changepassword")]
    public async Task<ActionResult<ApiResponse>> ChangePassword(int id, [FromBody] UsersChangePasswordRequest request)
    {
        try
        {
            var changed = await _usersService.ChangePasswordAsync(id, request);
            if (!changed)
            {
                return BadRequest(ApiResponse.FailResponse("Failed to change password. Old password may be incorrect.", 400));
            }

            _logger.LogInformation("Changed password for user with id '{Id}'.", id);
            return Ok(ApiResponse.SuccessResponse("Password changed successfully", 200));
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResponse.FailResponse(ex.Message, 400));
        }
    }

    private static UsersGetAllResponse MapToGetAllResponse(Users user) => new()
    {
        Id = user.Id,
        Username = user.Username,
        Email = user.Email,
        FullName = user.FullName,
        PhoneNumber = user.PhoneNumber,
        AvatarUrl = user.AvatarUrl,
        BravoId = user.BravoId,
        OrganizationUnitId = user.OrganizationUnitId,
        OrganizationUnitName = user.OrganizationUnit?.Name,
        IsActive = user.IsActive,
        IsLocked = user.IsLocked,
        LockoutEnd = user.LockoutEnd,
        FailedLoginCount = user.FailedLoginCount,
        LastLoginAt = user.LastLoginAt,
        CreatedAt = user.CreatedAt,
        CreatedBy = user.CreatedBy,
        UpdatedAt = user.UpdatedAt,
        UpdatedBy = user.UpdatedBy,
        IsDeleted = user.IsDeleted
    };

    private static UsersGetByIdResponse MapToGetByIdResponse(Users user) => new()
    {
        Id = user.Id,
        Username = user.Username,
        Email = user.Email,
        FullName = user.FullName,
        PhoneNumber = user.PhoneNumber,
        AvatarUrl = user.AvatarUrl,
        BravoId = user.BravoId,
        OrganizationUnitId = user.OrganizationUnitId,
        OrganizationUnitName = user.OrganizationUnit?.Name,
        IsActive = user.IsActive,
        IsLocked = user.IsLocked,
        LockoutEnd = user.LockoutEnd,
        FailedLoginCount = user.FailedLoginCount,
        LastLoginAt = user.LastLoginAt,
        CreatedAt = user.CreatedAt,
        CreatedBy = user.CreatedBy,
        UpdatedAt = user.UpdatedAt,
        UpdatedBy = user.UpdatedBy,
        IsDeleted = user.IsDeleted
    };

    private static UsersCreateResponse MapToCreateResponse(Users user) => new()
    {
        Id = user.Id,
        Username = user.Username,
        Email = user.Email,
        FullName = user.FullName,
        PhoneNumber = user.PhoneNumber,
        AvatarUrl = user.AvatarUrl,
        BravoId = user.BravoId,
        OrganizationUnitId = user.OrganizationUnitId,
        OrganizationUnitName = user.OrganizationUnit?.Name,
        IsActive = user.IsActive,
        IsLocked = user.IsLocked,
        LockoutEnd = user.LockoutEnd,
        FailedLoginCount = user.FailedLoginCount,
        LastLoginAt = user.LastLoginAt,
        CreatedAt = user.CreatedAt,
        CreatedBy = user.CreatedBy,
        UpdatedAt = user.UpdatedAt,
        UpdatedBy = user.UpdatedBy,
        IsDeleted = user.IsDeleted
    };

    private static UsersUpdateResponse MapToUpdateResponse(Users user) => new()
    {
        Id = user.Id,
        Username = user.Username,
        Email = user.Email,
        FullName = user.FullName,
        PhoneNumber = user.PhoneNumber,
        AvatarUrl = user.AvatarUrl,
        BravoId = user.BravoId,
        OrganizationUnitId = user.OrganizationUnitId,
        OrganizationUnitName = user.OrganizationUnit?.Name,
        IsActive = user.IsActive,
        IsLocked = user.IsLocked,
        LockoutEnd = user.LockoutEnd,
        FailedLoginCount = user.FailedLoginCount,
        LastLoginAt = user.LastLoginAt,
        CreatedAt = user.CreatedAt,
        CreatedBy = user.CreatedBy,
        UpdatedAt = user.UpdatedAt,
        UpdatedBy = user.UpdatedBy,
        IsDeleted = user.IsDeleted
    };
}
