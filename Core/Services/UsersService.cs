using Core.Helpers;
using Core.Interfaces;
using Data.DbContexts;
using Domain.Entities.FlyOps;
using Domain.Entities.Identity;
using Hgs.Share.Requests.Users;
using Microsoft.EntityFrameworkCore;

namespace Core.Services;

public class UsersService : IUsersService
{
    private readonly HgsDbContext _dbContext;
    private readonly FlyOpsDbContext _flyOpsDbContext;

    public UsersService(HgsDbContext dbContext, FlyOpsDbContext flyOpsDbContext)
    {
        _dbContext = dbContext;
        _flyOpsDbContext = flyOpsDbContext;
    }

    public async Task<IEnumerable<Users>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.Users
            .Include(x => x.OrganizationUnit)
            .Where(x => !x.IsDeleted)
            .AsNoTracking()
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
        return await _dbContext.Users
            .Include(x => x.OrganizationUnit)
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, cancellationToken);
    }

    public async Task<Users> CreateAsync(UsersCreateRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
        {
            throw new ArgumentException("Username and password are required");
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
        return true;
    }


}
