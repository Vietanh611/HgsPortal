using Core.Interfaces;
using Domain.Entities.Identity;
using Hgs.Share.Requests;
using Hgs.Share.Requests.Users;
using Hgs.Share.Responses;
using Hgs.Share.Responses.ApiResponses;
using Hgs.Share.Responses.Users;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace API.Controllers;

[ApiController]
[Route("[controller]")]
[IgnoreAntiforgeryToken]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IAuthService authService, ILogger<AuthController> logger)
    {
        _authService = authService;
        _logger = logger;
    }

    [HttpPost("login")]
    public async Task<ActionResult<ApiResponse<AuthenticateResponse>>> Login(
        [FromBody] AuthenticateRequest request,
        CancellationToken cancellationToken)
    {
        var response = await _authService.LoginAsync(
            request,
            HttpContext.Request.Headers.UserAgent.ToString(),
            HttpContext.Connection.RemoteIpAddress?.ToString(),
            cancellationToken);

        _logger.LogInformation("User '{Username}' authenticated successfully.", request.Username);
        return Ok(ApiResponse<AuthenticateResponse>.SuccessResponse(response, "Login successful", 200));
    }

    [HttpPost("refresh-token")]
    public async Task<ActionResult<ApiResponse<AuthenticateResponse>>> RefreshToken(
        [FromBody] RefreshTokenRequest request,
        CancellationToken cancellationToken)
    {
        var response = await _authService.RefreshTokenAsync(
            request,
            HttpContext.Request.Headers.UserAgent.ToString(),
            HttpContext.Connection.RemoteIpAddress?.ToString(),
            cancellationToken);

        return Ok(ApiResponse<AuthenticateResponse>.SuccessResponse(response, "Token refreshed successfully", 200));
    }

    [HttpPost("logout")]
    public async Task<ActionResult<ApiResponse>> Logout(
        [FromBody] LogoutRequest request,
        CancellationToken cancellationToken)
    {
        await _authService.LogoutAsync(request, cancellationToken);
        return Ok(ApiResponse.SuccessResponse("Logout successful", 200));
    }

    [HttpGet("me")]
    public async Task<ActionResult<ApiResponse<UsersGetByIdResponse>>> GetCurrentUser(CancellationToken cancellationToken)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrWhiteSpace(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized(ApiResponse<UsersGetByIdResponse>.FailResponse("Invalid user token", 401));
        }

        var user = await _authService.GetCurrentUserAsync(userId, cancellationToken);
        if (user is null)
        {
            return NotFound(ApiResponse<UsersGetByIdResponse>.FailResponse("User not found", 404));
        }

        return Ok(ApiResponse<UsersGetByIdResponse>.SuccessResponse(MapToUserResponse(user), "User retrieved successfully", 200));
    }

    private static UsersGetByIdResponse MapToUserResponse(Users user) => new()
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
