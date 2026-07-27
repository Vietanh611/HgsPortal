using Core.Interfaces;
using Data.DbContexts;
using Domain.Entities.Identity;
using Hgs.Share.Requests;
using Hgs.Share.Requests.Users;
using Hgs.Share.Responses;
using Hgs.Share.Responses.ApiResponses;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography;

namespace API.Controllers;

[ApiController]
[Route("[controller]")]
public class AuthController : ControllerBase
{
    private readonly HgsDbContext _dbContext;
    private readonly ITokenService _tokenService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(HgsDbContext dbContext, ITokenService tokenService, ILogger<AuthController> logger)
    {
        _dbContext = dbContext;
        _tokenService = tokenService;
        _logger = logger;
    }

    [HttpPost("login")]
    public async Task<ActionResult<ApiResponse<AuthenticateResponse>>> Login([FromBody] AuthenticateRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
        {
            _logger.LogWarning("Login attempt with missing username or password.");
            return BadRequest(ApiResponse<AuthenticateResponse>.FailResponse("Username and password are required.", 400));
        }

        var user = await _dbContext.Users
            .Where(u => u.Username == request.Username && !u.IsDeleted)
            .FirstOrDefaultAsync();

        if (user is null || !VerifyPassword(request.Password, user.PasswordHash))
        {
            _logger.LogWarning("Invalid login attempt for user '{Username}'.", request.Username);
            return Unauthorized(ApiResponse<AuthenticateResponse>.FailResponse("Invalid username or password.", 401));
        }

        if (!user.IsActive)
        {
            _logger.LogWarning("Inactive user '{Username}' tried to login.", request.Username);
            return Unauthorized(ApiResponse<AuthenticateResponse>.FailResponse("User is not active.", 401));
        }

        _logger.LogInformation("User '{Username}' authenticated successfully.", request.Username);

        var accessToken = _tokenService.GenerateAccessToken(user.Id, user.Username);
        var accessJwtId = new JwtSecurityTokenHandler().ReadJwtToken(accessToken).Id;
        var refreshToken = _tokenService.GenerateRefreshToken();

        await SaveRefreshTokenAsync(user.Id, refreshToken, accessJwtId);

        return Ok(ApiResponse<AuthenticateResponse>.SuccessResponse(new AuthenticateResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpiresAt = DateTime.UtcNow.AddMinutes(60)
        }, "Login successful", 200));
    }

    [HttpPost("refresh-token")]
    public async Task<ActionResult<ApiResponse<AuthenticateResponse>>> RefreshToken([FromBody] RefreshTokenRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
        {
            return BadRequest(ApiResponse<AuthenticateResponse>.FailResponse("Refresh token is required.", 400));
        }

        var tokenEntity = await _dbContext.RefreshTokens
            .Include(rt => rt.User)
            .Where(rt => rt.Token == request.RefreshToken)
            .FirstOrDefaultAsync();

        if (tokenEntity is null || tokenEntity.IsRevoked || tokenEntity.ExpiresAt < DateTime.UtcNow)
        {
            return Unauthorized(ApiResponse<AuthenticateResponse>.FailResponse("Invalid or expired refresh token.", 401));
        }

        var user = tokenEntity.User;
        if (user is null || user.IsDeleted || !user.IsActive)
        {
            return Unauthorized(ApiResponse<AuthenticateResponse>.FailResponse("Invalid user for refresh token.", 401));
        }

        var newAccessToken = _tokenService.GenerateAccessToken(user.Id, user.Username);
        var newJwtId = new JwtSecurityTokenHandler().ReadJwtToken(newAccessToken).Id;
        var newRefreshToken = _tokenService.GenerateRefreshToken();

        tokenEntity.IsRevoked = true;
        tokenEntity.RevokedAt = DateTime.UtcNow;
        tokenEntity.ReplacedByToken = newRefreshToken;

        await SaveRefreshTokenAsync(user.Id, newRefreshToken, newJwtId);
        await _dbContext.SaveChangesAsync();

        return Ok(ApiResponse<AuthenticateResponse>.SuccessResponse(new AuthenticateResponse
        {
            AccessToken = newAccessToken,
            RefreshToken = newRefreshToken,
            ExpiresAt = DateTime.UtcNow.AddMinutes(60)
        }, "Token refreshed successfully", 200));
    }

    [HttpPost("logout")]
    public async Task<ActionResult<ApiResponse>> Logout([FromBody] LogoutRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
        {
            return BadRequest(ApiResponse.FailResponse("Refresh token is required.", 400));
        }

        var tokenEntity = await _dbContext.RefreshTokens
            .Where(rt => rt.Token == request.RefreshToken)
            .FirstOrDefaultAsync();

        if (tokenEntity is null)
        {
            return NotFound(ApiResponse.FailResponse("Refresh token not found.", 404));
        }

        if (!tokenEntity.IsRevoked)
        {
            tokenEntity.IsRevoked = true;
            tokenEntity.RevokedAt = DateTime.UtcNow;
        }

        await _dbContext.SaveChangesAsync();
        return Ok(ApiResponse.SuccessResponse("Logout successful", 200));
    }

    private async Task SaveRefreshTokenAsync(int userId, string refreshToken, string jwtId)
    {
        var tokenEntity = new RefreshTokens
        {
            UserId = userId,
            Token = refreshToken,
            JwtId = jwtId,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            IsRevoked = false,
            CreatedByIp = HttpContext.Connection.RemoteIpAddress?.ToString()
        };

        await _dbContext.RefreshTokens.AddAsync(tokenEntity);
        await _dbContext.SaveChangesAsync();
    }

    private static bool VerifyPassword(string password, string storedHash)
    {
        if (string.IsNullOrWhiteSpace(storedHash))
        {
            return false;
        }

        var hashBytes = Convert.FromBase64String(storedHash);
        var salt = new byte[16];
        Buffer.BlockCopy(hashBytes, 0, salt, 0, 16);

        using var pbkdf2 = new Rfc2898DeriveBytes(password, salt, 100_000, HashAlgorithmName.SHA256);
        var hash = pbkdf2.GetBytes(32);

        for (var i = 0; i < 32; i++)
        {
            if (hashBytes[i + 16] != hash[i])
            {
                return false;
            }
        }

        return true;
    }
}
