using API.Authorization;
using Core.Interfaces;
using Core.Services.Settings;
using Domain.Entities.FlyOps;
using Domain.Entities.Identity;
using Hgs.Share.Requests.Users;
using Hgs.Share.Responses.ApiResponses;
using Hgs.Share.Responses.Users;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Security.Claims;

namespace API.Controllers;

[MenuPermission("USERS")]
public class UsersController : BaseApiController
{
    private readonly IUsersService _usersService;
    private readonly ILogger<UsersController> _logger;
    private readonly IWebHostEnvironment _env;
    private readonly StorageSettings _storage;

    public UsersController(
        IUsersService usersService,
        ILogger<UsersController> logger,
        IWebHostEnvironment env,
        IOptions<StorageSettings> storageOptions)
    {
        _usersService = usersService;
        _logger = logger;
        _env = env;
        _storage = storageOptions.Value;
    }

    /// <summary>
    /// Trả về danh sách user thuộc phạm vi tổ chức (org-scope) của người gọi;
    /// superadmin thấy toàn bộ user chưa bị xóa mềm.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<ApiResponse<IEnumerable<UsersGetAllResponse>>>> GetAll()
    {
        var users = await _usersService.GetAllAsync();
        var response = users.Select(MapToGetAllResponse).ToList();

        return Ok(ApiResponse<IEnumerable<UsersGetAllResponse>>.SuccessResponse(response, "Users retrieved successfully", 200));
    }

    /// <summary>
    /// Trả 404 thay vì 403 khi user ngoài org-scope để không lộ thông tin về sự tồn tại của user.
    /// </summary>
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

    /// <summary>
    /// Danh sách nhân viên lấy từ hệ thống Bravo (bảng NhanVien, FlyOps) chưa nghỉ việc —
    /// nguồn dữ liệu khác hệ thống HGS.
    /// </summary>
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

    /// <summary>
    /// Tạo user mới; org đích phải nằm trong org-scope của người gọi, ngoài phạm vi → 403.
    /// </summary>
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
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized access to create user");
            return StatusCode(403, ApiResponse<UsersCreateResponse>.FailResponse("Bạn không có quyền thực hiện thao tác này", 403));
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(ApiResponse<UsersCreateResponse>.FailResponse(ex.Message, 409));
        }
    }

    /// <summary>
    /// Cập nhật user trong org-scope; nếu đổi OrganizationUnitId, org mới cũng phải trong phạm vi → 403.
    /// </summary>
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
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized access to update user");
            return StatusCode(403, ApiResponse<UsersUpdateResponse>.FailResponse("Bạn không có quyền thực hiện thao tác này", 403));
        }
    }

    /// <summary>
    /// Xóa mềm (đánh dấu IsDeleted, không xóa vật lý); user đích phải nằm trong org-scope → 403.
    /// </summary>
    [HttpDelete("{id:int}")]
    public async Task<ActionResult<ApiResponse>> Delete(int id)
    {
        try
        {
            var deleted = await _usersService.DeleteAsync(id);
            if (!deleted)
            {
                return NotFound(ApiResponse.FailResponse("User not found", 404));
            }

            _logger.LogInformation("Deleted user with id '{Id}'.", id);
            return Ok(ApiResponse.SuccessResponse("User deleted successfully", 200));
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized access to delete user");
            return StatusCode(403, ApiResponse.FailResponse("Bạn không có quyền thực hiện thao tác này", 403));
        }
    }

    /// <summary>
    /// Chỉ người sở hữu tài khoản đổi được mật khẩu vì phải xác thực bằng mật khẩu hiện tại
    /// trước khi cập nhật; sai mật khẩu hiện tại → 400.
    /// </summary>
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

    /// <summary>
    /// Đặt lại mật khẩu cho user trong org-scope (thao tác quản trị, không cần mật khẩu hiện tại);
    /// ngoài phạm vi → 403.
    /// </summary>
    [HttpPut("{id:int}/resetpassword")]
    public async Task<ActionResult<ApiResponse>> ResetPassword(int id, [FromBody] UsersResetPasswordRequest request)
    {
        try
        {
            var reset = await _usersService.ResetPasswordAsync(id, request);
            if (!reset)
            {
                return NotFound(ApiResponse.FailResponse("User not found", 404));
            }

            _logger.LogInformation("Reset password for user with id '{Id}'.", id);
            return Ok(ApiResponse.SuccessResponse("Password reset successfully", 200));
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized access to reset password");
            return StatusCode(403, ApiResponse.FailResponse("Bạn không có quyền thực hiện thao tác này", 403));
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResponse.FailResponse(ex.Message, 400));
        }
    }

    /// <summary>
    /// Upload avatar cho user trong org-scope; giới hạn kích thước 5 MB (RequestSizeLimit) và
    /// chỉ chấp nhận định dạng trong cấu hình StorageSettings.
    /// </summary>
    [HttpPost("{id:int}/avatar")]
    [RequestSizeLimit(5 * 1024 * 1024)]
    public async Task<ActionResult<ApiResponse<UsersUpdateResponse>>> UploadAvatar(int id, IFormFile file)
    {
        try
        {
            if (file is null || file.Length == 0)
            {
                return BadRequest(ApiResponse<UsersUpdateResponse>.FailResponse("Vui lòng chọn tệp ảnh.", 400));
            }

            var avatarDirectory = Path.Combine(_env.ContentRootPath, _storage.AvatarDirectory);
            var avatarUrl = await _usersService.UploadAvatarAsync(
                id,
                file.OpenReadStream(),
                file.FileName,
                file.ContentType,
                avatarDirectory);

            if (avatarUrl is null)
            {
                return NotFound(ApiResponse<UsersUpdateResponse>.FailResponse("User not found", 404));
            }

            var user = await _usersService.GetByIdAsync(id);
            _logger.LogInformation("Uploaded avatar for user with id '{Id}'.", id);
            return Ok(ApiResponse<UsersUpdateResponse>.SuccessResponse(MapToUpdateResponse(user!), "Avatar uploaded successfully", 200));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ApiResponse<UsersUpdateResponse>.FailResponse(ex.Message, 400));
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized access to upload avatar");
            return StatusCode(403, ApiResponse<UsersUpdateResponse>.FailResponse("Bạn không có quyền thực hiện thao tác này", 403));
        }
    }

    private UsersGetAllResponse MapToGetAllResponse(Users user) => new()
    {
        Id = user.Id,
        Username = user.Username,
        Email = user.Email,
        FullName = user.FullName,
        PhoneNumber = user.PhoneNumber,
        AvatarUrl = ResolveUrlPath(user.AvatarUrl),
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

    private UsersGetByIdResponse MapToGetByIdResponse(Users user) => new()
    {
        Id = user.Id,
        Username = user.Username,
        Email = user.Email,
        FullName = user.FullName,
        PhoneNumber = user.PhoneNumber,
        AvatarUrl = ResolveUrlPath(user.AvatarUrl),
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

    private UsersCreateResponse MapToCreateResponse(Users user) => new()
    {
        Id = user.Id,
        Username = user.Username,
        Email = user.Email,
        FullName = user.FullName,
        PhoneNumber = user.PhoneNumber,
        AvatarUrl = ResolveUrlPath(user.AvatarUrl),
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

    private UsersUpdateResponse MapToUpdateResponse(Users user) => new()
    {
        Id = user.Id,
        Username = user.Username,
        Email = user.Email,
        FullName = user.FullName,
        PhoneNumber = user.PhoneNumber,
        AvatarUrl = ResolveUrlPath(user.AvatarUrl),
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
