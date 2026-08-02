using Core.Helpers;
using Core.Interfaces;
using Data.DbContexts;
using Domain.Entities.Identity;
using Hgs.Share.Exceptions;
using Hgs.Share.Requests;
using Hgs.Share.Requests.Users;
using Hgs.Share.Responses;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.IdentityModel.Tokens.Jwt;

namespace Core.Services;

public class AuthService : IAuthService
{
    private readonly HgsDbContext _dbContext;
    private readonly ITokenService _tokenService;
    private readonly JwtSettings _jwtSettings;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        HgsDbContext dbContext,
        ITokenService tokenService,
        IOptions<JwtSettings> jwtSettings,
        ILogger<AuthService> logger)
    {
        _dbContext = dbContext;
        _tokenService = tokenService;
        _jwtSettings = jwtSettings.Value;
        _logger = logger;
    }

    public async Task<AuthenticateResponse> LoginAsync(
        AuthenticateRequest request,
        string? userAgent,
        string? ipAddress,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
        {
            _logger.LogWarning("Login attempt with missing username or password.");
            throw new BadRequestException("Username and password are required.");
        }

        var user = await _dbContext.Users
            .Where(u => u.Username == request.Username && !u.IsDeleted)
            .FirstOrDefaultAsync(cancellationToken);

        if (user is null || !PasswordHelper.VerifyPassword(request.Password, user.PasswordHash))
        {
            _logger.LogWarning("Invalid login attempt for user '{Username}'.", request.Username);
            throw new UnauthorizedException("Invalid username or password.");
        }

        if (!user.IsActive)
        {
            _logger.LogWarning("Inactive user '{Username}' tried to login.", request.Username);
            throw new UnauthorizedException("User is not active.");
        }
        user.LastLoginAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);
        return await IssueTokensAsync(user, userAgent, ipAddress, cancellationToken);
    }

    public async Task<AuthenticateResponse> RefreshTokenAsync(
        RefreshTokenRequest request,
        string? userAgent,
        string? ipAddress,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
        {
            throw new BadRequestException("Refresh token is required.");
        }

        var tokenEntity = await _dbContext.RefreshTokens
            .Include(rt => rt.User)
            .Where(rt => rt.Token == request.RefreshToken)
            .FirstOrDefaultAsync(cancellationToken);

        if (tokenEntity is null || tokenEntity.IsRevoked || tokenEntity.ExpiresAt < DateTime.UtcNow)
        {
            throw new UnauthorizedException("Invalid or expired refresh token.");
        }

        var user = tokenEntity.User;
        if (user is null || user.IsDeleted || !user.IsActive)
        {
            throw new UnauthorizedException("Invalid user for refresh token.");
        }

        var newAccessToken = _tokenService.GenerateAccessToken(user.Id, user.Username);
        var newJwtId = new JwtSecurityTokenHandler().ReadJwtToken(newAccessToken).Id;
        var newRefreshToken = _tokenService.GenerateRefreshToken();

        tokenEntity.IsRevoked = true;
        tokenEntity.RevokedAt = DateTime.UtcNow;
        tokenEntity.ReplacedByToken = newRefreshToken;

        await SaveRefreshTokenAsync(user.Id, newRefreshToken, newJwtId, userAgent, ipAddress, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return BuildAuthenticateResponse(newAccessToken, newRefreshToken);
    }

    public async Task LogoutAsync(LogoutRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
        {
            throw new BadRequestException("Refresh token is required.");
        }

        var tokenEntity = await _dbContext.RefreshTokens
            .Where(rt => rt.Token == request.RefreshToken)
            .FirstOrDefaultAsync(cancellationToken);

        if (tokenEntity is null)
        {
            throw new NotFoundException("Refresh token not found.");
        }

        if (!tokenEntity.IsRevoked)
        {
            tokenEntity.IsRevoked = true;
            tokenEntity.RevokedAt = DateTime.UtcNow;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task<AuthenticateResponse> IssueTokensAsync(
        Users user,
        string? userAgent,
        string? ipAddress,
        CancellationToken cancellationToken)
    {
        var accessToken = _tokenService.GenerateAccessToken(user.Id, user.Username);
        var accessJwtId = new JwtSecurityTokenHandler().ReadJwtToken(accessToken).Id;
        var refreshToken = _tokenService.GenerateRefreshToken();

        await SaveRefreshTokenAsync(user.Id, refreshToken, accessJwtId, userAgent, ipAddress, cancellationToken);

        return BuildAuthenticateResponse(accessToken, refreshToken);
    }

    private AuthenticateResponse BuildAuthenticateResponse(string accessToken, string refreshToken) => new()
    {
        AccessToken = accessToken,
        RefreshToken = refreshToken,
        ExpiresAt = DateTime.UtcNow.AddMinutes(_jwtSettings.ExpiryMinutes)
    };

    private async Task SaveRefreshTokenAsync(
        int userId,
        string refreshToken,
        string jwtId,
        string? userAgent,
        string? ipAddress,
        CancellationToken cancellationToken)
    {
        var tokenEntity = new RefreshTokens
        {
            UserId = userId,
            Token = refreshToken,
            JwtId = jwtId,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            UserAgent = userAgent,
            IsRevoked = false,
            CreatedByIp = ipAddress
        };

        await _dbContext.RefreshTokens.AddAsync(tokenEntity, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<Users?> GetCurrentUserAsync(int userId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Users
            .Include(x => x.OrganizationUnit)
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == userId && !x.IsDeleted, cancellationToken);
    }
}
