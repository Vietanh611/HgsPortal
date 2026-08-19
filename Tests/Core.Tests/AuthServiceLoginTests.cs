using Core.Helpers;
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
/// Test AuthService.LoginAsync với EF Core InMemory. Bao phủ cơ chế lockout: đăng nhập sai đủ
/// ngưỡng → tài khoản bị khóa (LockoutEnd + IsLocked), user đang bị khóa luôn nhận
/// <see cref="AccountLockedException"/> bất kể mật khẩu đúng/sai, lockout hết hạn được tự gỡ.
/// </summary>
public class AuthServiceLoginTests
{
    private const string ValidPassword = "CorrectPassword123";
    private const string WrongPassword = "WrongPassword";

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

    private static AuthService CreateService(HgsDbContext db, IAuditLogService auditLog)
    {
        return new AuthService(
            db,
            new FakeTokenService(),
            mailService: null!,
            auditLog: auditLog,
            notificationService: null!,
            menuService: null!,
            Options.Create(new JwtSettings { ExpiryMinutes = 10, RefreshReuseIntervalSeconds = 60 }),
            Options.Create(new CookieSettings { Secure = false, SameSite = SameSiteMode.Lax }),
            Options.Create(new LockoutSettings { MaxFailedAttempts = 5, LockoutMinutes = 15 }),
            Options.Create(new MailSettings()),
            NullLogger<AuthService>.Instance);
    }

    private static async Task<Users> SeedActiveUserAsync(HgsDbContext db, string username = "user1")
    {
        var user = new Users
        {
            Id = 1,
            Username = username,
            Email = "user1@example.com",
            PasswordHash = PasswordHelper.HashPassword(ValidPassword),
            FullName = "User One",
            OrganizationUnitId = 1,
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();
        return user;
    }

    [Fact]
    public async Task LoginAsync_WrongPasswordReachesThreshold_LocksAccount()
    {
        var db = CreateDb(nameof(LoginAsync_WrongPasswordReachesThreshold_LocksAccount));
        var service = CreateService(db, new FakeAuditLogService());
        await SeedActiveUserAsync(db);

        for (var i = 0; i < 5; i++)
        {
            await Assert.ThrowsAsync<UnauthorizedException>(() => service.LoginAsync(
                new AuthenticateRequest { Username = "user1", Password = WrongPassword },
                "desktop", "127.0.0.1"));
        }

        var user = await db.Users.SingleAsync();
        Assert.True(user.IsLocked);
        Assert.NotNull(user.LockoutEnd);
        Assert.True(user.LockoutEnd > DateTime.UtcNow);
    }

    [Fact]
    public async Task LoginAsync_LockedUser_CorrectPassword_ThrowsAccountLocked()
    {
        var db = CreateDb(nameof(LoginAsync_LockedUser_CorrectPassword_ThrowsAccountLocked));
        var audit = new FakeAuditLogService();
        var service = CreateService(db, audit);
        var user = await SeedActiveUserAsync(db);
        user.IsLocked = true;
        user.LockoutEnd = DateTime.UtcNow.AddMinutes(15);
        await db.SaveChangesAsync();

        var ex = await Assert.ThrowsAsync<AccountLockedException>(() => service.LoginAsync(
            new AuthenticateRequest { Username = "user1", Password = ValidPassword },
            "desktop", "127.0.0.1"));

        Assert.Contains("đã bị khóa", ex.Message);
        Assert.Equal("ACCOUNT_LOCKED", ex.ErrorCode);
        Assert.Contains("LOGIN_FAIL_LOCKED", audit.SecurityEvents);
    }

    [Fact]
    public async Task LoginAsync_LockedUser_WrongPassword_StillThrowsAccountLocked()
    {
        var db = CreateDb(nameof(LoginAsync_LockedUser_WrongPassword_StillThrowsAccountLocked));
        var service = CreateService(db, new FakeAuditLogService());
        var user = await SeedActiveUserAsync(db);
        user.IsLocked = true;
        user.LockoutEnd = DateTime.UtcNow.AddMinutes(15);
        await db.SaveChangesAsync();

        var ex = await Assert.ThrowsAsync<AccountLockedException>(() => service.LoginAsync(
            new AuthenticateRequest { Username = "user1", Password = WrongPassword },
            "desktop", "127.0.0.1"));

        Assert.Equal("ACCOUNT_LOCKED", ex.ErrorCode);

        // Không gia hạn thêm thời gian khóa khi nhập sai trong lúc đã khóa
        var userAfter = await db.Users.SingleAsync();
        Assert.Equal(0, userAfter.FailedLoginCount);
        Assert.True(userAfter.LockoutEnd > DateTime.UtcNow);
    }

    [Fact]
    public async Task LoginAsync_ExpiredLockout_CorrectPassword_SucceedsAndClearsLock()
    {
        var db = CreateDb(nameof(LoginAsync_ExpiredLockout_CorrectPassword_SucceedsAndClearsLock));
        var service = CreateService(db, new FakeAuditLogService());
        var user = await SeedActiveUserAsync(db);
        user.IsLocked = true;
        user.LockoutEnd = DateTime.UtcNow.AddMinutes(-1);
        await db.SaveChangesAsync();

        var response = await service.LoginAsync(
            new AuthenticateRequest { Username = "user1", Password = ValidPassword },
            "desktop", "127.0.0.1");

        Assert.False(string.IsNullOrEmpty(response.AccessToken));

        var userAfter = await db.Users.SingleAsync();
        Assert.False(userAfter.IsLocked);
        Assert.Null(userAfter.LockoutEnd);
        Assert.Equal(0, userAfter.FailedLoginCount);
    }

    [Fact]
    public async Task LoginAsync_ExpiredLockout_WrongPassword_RestartsCounter()
    {
        var db = CreateDb(nameof(LoginAsync_ExpiredLockout_WrongPassword_RestartsCounter));
        var service = CreateService(db, new FakeAuditLogService());
        var user = await SeedActiveUserAsync(db);
        user.IsLocked = true;
        user.LockoutEnd = DateTime.UtcNow.AddMinutes(-1);
        user.FailedLoginCount = 5;
        await db.SaveChangesAsync();

        await Assert.ThrowsAsync<UnauthorizedException>(() => service.LoginAsync(
            new AuthenticateRequest { Username = "user1", Password = WrongPassword },
            "desktop", "127.0.0.1"));

        // Lockout hết hạn đã được gỡ → đếm lỗi bắt đầu lại từ 1, chưa bị khóa lại
        var userAfter = await db.Users.SingleAsync();
        Assert.False(userAfter.IsLocked);
        Assert.Null(userAfter.LockoutEnd);
        Assert.Equal(1, userAfter.FailedLoginCount);
    }
}
