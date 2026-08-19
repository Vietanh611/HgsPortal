using Core.Helpers;
using Core.Interfaces.Identity;
using Core.Interfaces.Operations;
using Core.Services.Settings;
using Data.DbContexts;
using Domain.Entities.FlyOps;
using Domain.Entities.Identity;
using Hgs.Share.Requests.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Core.Services.Identity;

public class UsersService : IUsersService
{
    private readonly HgsDbContext _dbContext;
    private readonly FlyOpsDbContext _flyOpsDbContext;
    private readonly IAuditLogService _auditLog;
    private readonly IOrgScopeService _orgScope;
    private readonly StorageSettings _storage;

    public UsersService(
        HgsDbContext dbContext,
        FlyOpsDbContext flyOpsDbContext,
        IAuditLogService auditLog,
        IOrgScopeService orgScope,
        IOptions<StorageSettings> storageOptions)
    {
        _dbContext = dbContext;
        _flyOpsDbContext = flyOpsDbContext;
        _auditLog = auditLog;
        _orgScope = orgScope;
        _storage = storageOptions.Value;
    }

    private static void EnsureScope(bool inScope)
    {
        if (!inScope)
        {
            throw new UnauthorizedAccessException("Bạn không có quyền thực hiện thao tác này");
        }
    }

    public async Task<IEnumerable<Users>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var scopePaths = await _orgScope.GetCallerScopePathsAsync(cancellationToken);

        if (scopePaths is null)
        {
            return await _dbContext.Users
                .Include(x => x.OrganizationUnit)
                .Where(x => !x.IsDeleted)
                .AsNoTracking()
                .ToListAsync(cancellationToken);
        }

        if (!scopePaths.Any())
        {
            return Enumerable.Empty<Users>();
        }

        return await _dbContext.Users
            .Include(x => x.OrganizationUnit)
            .AsNoTracking()
            .Where(x => !x.IsDeleted)
            .Where(u => u.OrganizationUnit != null &&
                        u.OrganizationUnit.Path != null &&
                        scopePaths.Any(path => u.OrganizationUnit.Path == path ||
                                               u.OrganizationUnit.Path.StartsWith(path + "/")))
            .ToListAsync(cancellationToken);
    }
    public async Task<IEnumerable<NhanVien>> GetAllBravoNhanVienAsync(CancellationToken cancellationToken = default)
    {
        return await _flyOpsDbContext.NhanVien.Where(x => x.ResignDate == null)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<Users?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var user = await _dbContext.Users
            .Include(x => x.OrganizationUnit)
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, cancellationToken);

        if (user is null)
        {
            return null;
        }

        if (!await _orgScope.IsUserInScopeAsync(id, cancellationToken))
        {
            return null;
        }

        return user;
    }

    public async Task<Users> CreateAsync(UsersCreateRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
        {
            throw new ArgumentException("Username and password are required");
        }

        if (!await _orgScope.IsOrgUnitInScopeAsync(request.OrganizationUnitId, cancellationToken))
        {
            throw new UnauthorizedAccessException("Bạn không có quyền thực hiện thao tác này");
        }

        var exists = await _dbContext.Users
            .AnyAsync(x => x.Username == request.Username && !x.IsDeleted, cancellationToken);
        if (exists)
        {
            throw new InvalidOperationException("Username already exists");
        }

        var user = new Users
        {
            Username = request.Username,
            Email = request.Email,
            PasswordHash = PasswordHelper.HashPassword(request.Password),
            FullName = request.FullName,
            PhoneNumber = request.PhoneNumber,
            AvatarUrl = request.AvatarUrl,
            OrganizationUnitId = request.OrganizationUnitId,
            IsActive = request.IsActive,
            IsLocked = false,
            FailedLoginCount = 0,
            CreatedAt = DateTime.UtcNow,
            IsDeleted = false
        };

        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _auditLog.Log(
            action: "CREATE",
            entityName: "Users",
            entityId: user.Id,
            oldValue: null,
            newValue: user);

        await _dbContext.SaveChangesAsync(cancellationToken);
        return user;
    }

    public async Task<Users?> UpdateAsync(int id, UsersUpdateRequest request, CancellationToken cancellationToken = default)
    {
        var user = await _dbContext.Users
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (user is null || user.IsDeleted)
        {
            return null;
        }

        EnsureScope(await _orgScope.IsUserInScopeAsync(id, cancellationToken));

        if (request.OrganizationUnitId.HasValue &&
            !await _orgScope.IsOrgUnitInScopeAsync(request.OrganizationUnitId.Value, cancellationToken))
        {
            throw new UnauthorizedAccessException("Bạn không có quyền thực hiện thao tác này");
        }

        var oldSnapshot = new
        {
            user.Id,
            user.Username,
            user.Email,
            user.FullName,
            user.PhoneNumber,
            user.AvatarUrl,
            user.OrganizationUnitId,
            user.IsActive,
            user.IsLocked,
            user.FailedLoginCount,
            user.IsDeleted
        };

        if (!string.IsNullOrWhiteSpace(request.Email))
        {
            user.Email = request.Email;
        }

        if (!string.IsNullOrWhiteSpace(request.FullName))
        {
            user.FullName = request.FullName;
        }

        if (request.PhoneNumber is not null)
        {
            user.PhoneNumber = request.PhoneNumber;
        }

        if (request.AvatarUrl is not null)
        {
            user.AvatarUrl = request.AvatarUrl;
        }

        if (request.OrganizationUnitId.HasValue)
        {
            user.OrganizationUnitId = request.OrganizationUnitId.Value;
        }

        if (request.IsActive.HasValue)
        {
            user.IsActive = request.IsActive.Value;
        }

        user.UpdatedAt = DateTime.UtcNow;

        _auditLog.Log(
            action: "UPDATE",
            entityName: "Users",
            entityId: user.Id,
            oldValue: oldSnapshot,
            newValue: user);

        await _dbContext.SaveChangesAsync(cancellationToken);
        return user;
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var user = await _dbContext.Users
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (user is null || user.IsDeleted)
        {
            return false;
        }

        EnsureScope(await _orgScope.IsUserInScopeAsync(id, cancellationToken));

        _auditLog.Log(
            action: "DELETE",
            entityName: "Users",
            entityId: user.Id,
            oldValue: user,
            newValue: null);

        user.IsDeleted = true;
        user.UpdatedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> ChangePasswordAsync(int id, UsersChangePasswordRequest request, CancellationToken cancellationToken = default)
    {
        var user = await _dbContext.Users
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (user is null || user.IsDeleted)
        {
            return false;
        }

        if (!PasswordHelper.VerifyPassword(request.CurrentPassword, user.PasswordHash))
        {
            return false;
        }

        user.PasswordHash = PasswordHelper.HashPassword(request.NewPassword);
        user.UpdatedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditLog.LogSecurityEventAsync(
            action: "PASSWORD_CHANGED",
            eventCategory: "Security", success: true, severity: "Warning",
            userId: user.Id, username: user.Username,
            detail: "Đổi mật khẩu",
            newValue: new { user.Id, user.Username, ChangedAt = DateTime.UtcNow });

        return true;
    }

    public async Task<bool> ResetPasswordAsync(int id, UsersResetPasswordRequest request, CancellationToken cancellationToken = default)
    {
        var user = await _dbContext.Users
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (user is null || user.IsDeleted)
        {
            return false;
        }

        EnsureScope(await _orgScope.IsUserInScopeAsync(id, cancellationToken));

        var oldSnapshot = new
        {
            user.Id,
            user.Username,
            user.Email,
            user.FullName,
            user.PhoneNumber,
            user.OrganizationUnitId,
            user.IsActive,
            user.IsLocked,
            user.FailedLoginCount,
            user.IsDeleted
        };

        user.PasswordHash = PasswordHelper.HashPassword(request.NewPassword);
        user.UpdatedAt = DateTime.UtcNow;

        _auditLog.Log(
            action: "UPDATE",
            entityName: "Users",
            entityId: user.Id,
            oldValue: oldSnapshot,
            newValue: user);

        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> UnlockAsync(int id, CancellationToken cancellationToken = default)
    {
        var user = await _dbContext.Users
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (user is null || user.IsDeleted)
        {
            return false;
        }

        EnsureScope(await _orgScope.IsUserInScopeAsync(id, cancellationToken));

        user.LockoutEnd = null;
        user.IsLocked = false;
        user.FailedLoginCount = 0;
        user.UpdatedAt = DateTime.UtcNow;

        await _auditLog.LogSecurityEventAsync(
            action: "USER_UNLOCKED",
            eventCategory: "Security", success: true, severity: "Warning",
            targetUserId: user.Id, username: user.Username,
            entityName: "Users", entityId: user.Id,
            detail: "Mở khóa tài khoản bị khóa do đăng nhập sai nhiều lần");

        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<string?> UploadAvatarAsync(int id, Stream fileStream, string fileName, string contentType, string avatarDirectory, CancellationToken cancellationToken = default)
    {
        var user = await _dbContext.Users
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (user is null || user.IsDeleted)
        {
            return null;
        }

        EnsureScope(await _orgScope.IsUserInScopeAsync(id, cancellationToken));

        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(extension) ||
            !_storage.AllowedAvatarExtensions.Contains(extension))
        {
            throw new ArgumentException("Định dạng ảnh không được hỗ trợ. Chỉ chấp nhận JPG, PNG, WEBP, GIF.");
        }

        if (fileStream.Length > _storage.MaxAvatarBytes)
        {
            throw new ArgumentException($"Kích thước ảnh vượt quá giới hạn {_storage.MaxAvatarBytes / (1024 * 1024)}MB.");
        }

        Directory.CreateDirectory(avatarDirectory);

        var avatarFileName = $"avatar_{user.Id}_{Guid.NewGuid():N}{extension}";
        var avatarPhysicalPath = Path.Combine(avatarDirectory, avatarFileName);
        var avatarRelativePath = $"/uploads/avatars/{avatarFileName}";

        await using (var file = new FileStream(avatarPhysicalPath, FileMode.Create, FileAccess.Write, FileShare.None))
        {
            await fileStream.CopyToAsync(file, cancellationToken);
        }

        var oldAvatarUrl = user.AvatarUrl;
        var oldSnapshot = new
        {
            user.Id,
            user.Username,
            user.Email,
            user.FullName,
            user.PhoneNumber,
            user.AvatarUrl,
            user.OrganizationUnitId,
            user.IsActive,
            user.IsLocked,
            user.FailedLoginCount,
            user.IsDeleted
        };

        user.AvatarUrl = avatarRelativePath;
        user.UpdatedAt = DateTime.UtcNow;

        _auditLog.Log(
            action: "UPDATE",
            entityName: "Users",
            entityId: user.Id,
            oldValue: oldSnapshot,
            newValue: user);

        await _dbContext.SaveChangesAsync(cancellationToken);

        if (!string.IsNullOrWhiteSpace(oldAvatarUrl) &&
            oldAvatarUrl.StartsWith("/uploads/avatars/"))
        {
            var oldFileName = Path.GetFileName(oldAvatarUrl);
            var oldPhysicalPath = Path.Combine(avatarDirectory, oldFileName);
            if (File.Exists(oldPhysicalPath))
            {
                File.Delete(oldPhysicalPath);
            }
        }

        return avatarRelativePath;
    }
}
