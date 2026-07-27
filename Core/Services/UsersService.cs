using Core.Interfaces;
using Domain.Entities.Identity;
using Hgs.Share.Requests.Users;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;

namespace Core.Services;

public class UsersService : IUsersService
{
    private readonly IAppDbContext _dbContext;

    public UsersService(IAppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IEnumerable<Users>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.Users
            .Where(x => !x.IsDeleted)
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
            PasswordHash = HashPassword(request.Password),
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

    private static string HashPassword(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(16);
        var pbkdf2 = new Rfc2898DeriveBytes(password, salt, 100_000, HashAlgorithmName.SHA256);
        var hash = pbkdf2.GetBytes(32);

        var bytes = new byte[48];
        Buffer.BlockCopy(salt, 0, bytes, 0, 16);
        Buffer.BlockCopy(hash, 0, bytes, 16, 32);

        return Convert.ToBase64String(bytes);
    }
}
