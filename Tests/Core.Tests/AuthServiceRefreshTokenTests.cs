using Core.Interfaces.Auth;
using Core.Interfaces.Operations;
using Core.Services.Auth;
using Core.Services.Settings;
using Data.DbContexts;
using Domain.Entities.Identity;
using Hgs.Share.Exceptions;
using Hgs.Share.Requests;
using Hgs.Share.Requests.Audit;
using Hgs.Share.Responses.ApiResponses;
using Hgs.Share.Responses.AuditLogs;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Xunit;

namespace Core.Tests;

/// <summary>
/// Test AuthService.RefreshTokenAsync với EF Core InMemory. Bao phủ rotation bình thường và cửa
/// sổ reuse (multi-tab): token vừa bị rotate được dùng lại trong cửa sổ → cấp token mới thay vì
/// thu hồi phiên, giữ chuỗi TokenFamily liên tục; dùng lại ngoài cửa sổ vẫn bị coi là đánh cắp.
/// </summary>
public class AuthServiceRefreshTokenTests
{
    private sealed class FakeTokenService : ITokenService
    {
        public string GenerateAccessToken(int userId, string username, IEnumerable<string>? roles = null, int? expiryMinutes = null)
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("test-secret-0123456789abcdef-0123456789abcdef"));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var token = new JwtSecurityToken(
                issuer: "HgsPortal",
                audience: "HgsPortalClient",
                claims: new[] { new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()) },
                expires: DateTime.UtcNow.AddMinutes(10),
                signingCredentials: credentials);
            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public string GenerateRefreshToken() => Guid.NewGuid().ToString("N");

        public string HashRefreshToken(string token) =>
            Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));

        public Guid GenerateTokenFamily() => Guid.NewGuid();

        public string GenerateDeviceKey() => throw new NotImplementedException();
        public string HashDeviceKey(string deviceKey) => throw new NotImplementedException();
        public string GeneratePairingCode() => throw new NotImplementedException();
        public string HashPairingCode(string pairingCode) => throw new NotImplementedException();
    }

    private sealed class FakeAuditLogService : IAuditLogService
    {
        public List<string> SecurityEvents { get; } = new();

        public void Log(string action, string entityName, int? entityId, object? oldValue, object? newValue) { }

        public Task LogSecurityEventAsync(
            string action, string eventCategory, bool success, string severity,
            int? userId = null, int? targetUserId = null, string? username = null,
            string? entityName = null, int? entityId = null, string? detail = null,
            object? oldValue = null, object? newValue = null, CancellationToken cancellationToken = default)
        {
            SecurityEvents.Add(action);
            return Task.CompletedTask;
        }

        public Task<(IEnumerable<AuditLogsGetAllResponse> Items, int TotalCount)> GetAllAsync(
            int pageNumber = 1, int pageSize = 10, CancellationToken cancellationToken = default) => throw new NotImplementedException();

        public Task<PagedResponse<AuditLogsGetAllResponse>> GetFilteredAsync(
            AuditLogsFilterRequest request, CancellationToken cancellationToken = default) => throw new NotImplementedException();

        public Task<long> CountAsync(AuditLogsFilterRequest request, CancellationToken cancellationToken = default) => throw new NotImplementedException();

        public Task<List<AuditLogs>> GetAllFilteredAsync(
            AuditLogsFilterRequest request, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    }

    private static HgsDbContext CreateDb(string dbName)
    {
        var options = new DbContextOptionsBuilder<HgsDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        return new HgsDbContext(options);
    }

    private static AuthService CreateService(HgsDbContext db, int reuseIntervalSeconds, IAuditLogService? auditLog = null)
    {
        return new AuthService(
            db,
            new FakeTokenService(),
            mailService: null!,
            auditLog: auditLog ?? null!,
            notificationService: null!,
            menuService: null!,
            Options.Create(new JwtSettings { ExpiryMinutes = 10, RefreshReuseIntervalSeconds = reuseIntervalSeconds }),
            Options.Create(new CookieSettings { Secure = false, SameSite = SameSiteMode.Lax }),
            Options.Create(new LockoutSettings { MaxFailedAttempts = 5, LockoutMinutes = 15 }),
            Options.Create(new MailSettings()),
            NullLogger<AuthService>.Instance);
    }

    private static async Task<Users> SeedActiveUserAsync(HgsDbContext db)
    {
        var user = new Users
        {
            Id = 1,
            Username = "user1",
            Email = "user1@example.com",
            PasswordHash = "hash",
            FullName = "User One",
            OrganizationUnitId = 1,
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();
        return user;
    }

    private static async Task SeedRefreshTokenAsync(HgsDbContext db, Users user, string refreshToken, Guid family)
    {
        db.RefreshTokens.Add(new RefreshTokens
        {
            UserId = user.Id,
            TokenHash = new FakeTokenService().HashRefreshToken(refreshToken),
            JwtId = "jti",
            UserAgent = "desktop",
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            IsRevoked = false,
            CreatedByIp = "127.0.0.1",
            TokenFamily = family
        });
        await db.SaveChangesAsync();
    }

    [Fact]
    public async Task RefreshTokenAsync_Rotates_AndKeepsSameFamily()
    {
        var db = CreateDb(nameof(RefreshTokenAsync_Rotates_AndKeepsSameFamily));
        var service = CreateService(db, 60);
        var user = await SeedActiveUserAsync(db);
        var tokenValue = new FakeTokenService().GenerateRefreshToken();
        var family = Guid.NewGuid();
        await SeedRefreshTokenAsync(db, user, tokenValue, family);

        var response = await service.RefreshTokenAsync(
            new RefreshTokenRequest { RefreshToken = tokenValue }, "desktop", "127.0.0.1");

        Assert.NotNull(response.AccessToken);
        Assert.False(string.IsNullOrEmpty(response.RefreshToken));

        var old = await db.RefreshTokens.SingleAsync(t => t.TokenHash == new FakeTokenService().HashRefreshToken(tokenValue));
        Assert.True(old.IsRevoked);
        Assert.NotNull(old.RevokedAt);

        var active = await db.RefreshTokens.SingleAsync(t => !t.IsRevoked);
        Assert.Equal(family, active.TokenFamily);
        Assert.Equal(user.Id, active.UserId);
        Assert.Equal(new FakeTokenService().HashRefreshToken(response.RefreshToken!), active.TokenHash);
    }

    [Fact]
    public async Task RefreshTokenAsync_ReuseWithinWindow_IssuesNewTokensInsteadOfRevokingSession()
    {
        var db = CreateDb(nameof(RefreshTokenAsync_ReuseWithinWindow_IssuesNewTokensInsteadOfRevokingSession));
        var service = CreateService(db, 60);
        var user = await SeedActiveUserAsync(db);
        var tokenValue = new FakeTokenService().GenerateRefreshToken();
        var family = Guid.NewGuid();
        await SeedRefreshTokenAsync(db, user, tokenValue, family);

        // Tab A refresh → token cũ bị rotate thành newTokenA
        var responseA = await service.RefreshTokenAsync(
            new RefreshTokenRequest { RefreshToken = tokenValue }, "desktop", "127.0.0.1");
        var newTokenA = responseA.RefreshToken;
        Assert.False(string.IsNullOrEmpty(newTokenA));

        // Tab B vẫn giữ token cũ, refresh ngay trong cửa sổ reuse → PHẢI thành công, không thu hồi phiên
        var responseB = await service.RefreshTokenAsync(
            new RefreshTokenRequest { RefreshToken = tokenValue }, "desktop", "127.0.0.1");

        Assert.NotNull(responseB.AccessToken);
        Assert.False(string.IsNullOrEmpty(responseB.RefreshToken));
        Assert.NotEqual(newTokenA, responseB.RefreshToken);

        // Chỉ còn đúng một token active, cùng family — chuỗi xoay liên tục
        var active = await db.RefreshTokens.Where(t => !t.IsRevoked).ToListAsync();
        var single = Assert.Single(active);
        Assert.Equal(family, single.TokenFamily);
        Assert.Equal(new FakeTokenService().HashRefreshToken(responseB.RefreshToken!), single.TokenHash);
    }

    [Fact]
    public async Task RefreshTokenAsync_ReuseBeyondWindow_IsTreatedAsTheft()
    {
        var db = CreateDb(nameof(RefreshTokenAsync_ReuseBeyondWindow_IsTreatedAsTheft));
        var audit = new FakeAuditLogService();
        var service = CreateService(db, reuseIntervalSeconds: 60, auditLog: audit);
        var user = await SeedActiveUserAsync(db);
        var oldToken = new FakeTokenService().GenerateRefreshToken();
        var activeToken = new FakeTokenService().GenerateRefreshToken();
        var family = Guid.NewGuid();

        // Token cũ bị revoke từ 5 phút trước (ngoài cửa sổ reuse 60s); token active là bản thay thế
        db.RefreshTokens.AddRange(
            new RefreshTokens
            {
                UserId = user.Id,
                TokenHash = new FakeTokenService().HashRefreshToken(oldToken),
                JwtId = "jti",
                UserAgent = "desktop",
                CreatedAt = DateTime.UtcNow.AddMinutes(-10),
                ExpiresAt = DateTime.UtcNow.AddDays(7),
                IsRevoked = true,
                RevokedAt = DateTime.UtcNow.AddMinutes(-5),
                CreatedByIp = "127.0.0.1",
                TokenFamily = family
            },
            new RefreshTokens
            {
                UserId = user.Id,
                TokenHash = new FakeTokenService().HashRefreshToken(activeToken),
                JwtId = "jti",
                UserAgent = "desktop",
                CreatedAt = DateTime.UtcNow.AddMinutes(-5),
                ExpiresAt = DateTime.UtcNow.AddDays(7),
                IsRevoked = false,
                CreatedByIp = "127.0.0.1",
                TokenFamily = family
            });
        await db.SaveChangesAsync();

        await Assert.ThrowsAsync<UnauthorizedException>(() => service.RefreshTokenAsync(
            new RefreshTokenRequest { RefreshToken = oldToken }, "desktop", "127.0.0.1"));

        Assert.Contains("REFRESH_TOKEN_REUSE_DETECTED", audit.SecurityEvents);

        // Token active không bị đụng tới — phiên vẫn sống
        var active = await db.RefreshTokens.SingleAsync(t => !t.IsRevoked);
        Assert.Equal(new FakeTokenService().HashRefreshToken(activeToken), active.TokenHash);
    }
}
