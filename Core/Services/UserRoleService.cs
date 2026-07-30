using Core.Interfaces;
using Data.DbContexts;
using Domain.Entities.Identity;
using Hgs.Share.Requests.UserRoles;
using Microsoft.EntityFrameworkCore;

namespace Core.Services;

public class UserRoleService : IUserRoleService
{
    private readonly HgsDbContext _dbContext;

    public UserRoleService(HgsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IEnumerable<UserRoles>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.UserRoles
            .Include(ur => ur.User)
            .Include(ur => ur.Role)
            .AsNoTracking()
            .OrderByDescending(ur => ur.AssignedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<UserRoles?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.UserRoles
            .Include(ur => ur.User)
            .Include(ur => ur.Role)
            .AsNoTracking()
            .FirstOrDefaultAsync(ur => ur.Id == id, cancellationToken);
    }

    public async Task<IEnumerable<UserRoles>> GetByUserIdAsync(int userId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.UserRoles
            .Include(ur => ur.User)
            .Include(ur => ur.Role)
            .Where(ur => ur.UserId == userId)
            .AsNoTracking()
            .OrderByDescending(ur => ur.AssignedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<UserRoles>> GetByRoleIdAsync(int roleId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.UserRoles
            .Include(ur => ur.User)
            .Include(ur => ur.Role)
            .Where(ur => ur.RoleId == roleId)
            .AsNoTracking()
            .OrderByDescending(ur => ur.AssignedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<UserRoles> CreateAsync(UserRolesCreateRequest request, int assignedBy, CancellationToken cancellationToken = default)
    {
        // Check if user exists
        var user = await _dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == request.UserId && !u.IsDeleted, cancellationToken);
        if (user is null)
        {
            throw new KeyNotFoundException($"User with ID {request.UserId} not found");
        }

        // Check if role exists
        var role = await _dbContext.Roles
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == request.RoleId, cancellationToken);
        if (role is null)
        {
            throw new KeyNotFoundException($"Role with ID {request.RoleId} not found");
        }

        // Check if user already has this role
        var existingAssignment = await _dbContext.UserRoles
            .AsNoTracking()
            .FirstOrDefaultAsync(ur => ur.UserId == request.UserId && ur.RoleId == request.RoleId, cancellationToken);
        if (existingAssignment is not null)
        {
            throw new InvalidOperationException($"User already has role {role.Name} assigned");
        }

        var userRole = new UserRoles
        {
            UserId = request.UserId,
            RoleId = request.RoleId,
            AssignedAt = DateTime.UtcNow,
            AssignedBy = assignedBy,
        };

        _dbContext.UserRoles.Add(userRole);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return userRole;
    }

    public async Task<UserRoles?> UpdateAsync(int id, UserRolesUpdateRequest request, CancellationToken cancellationToken = default)
    {
        var userRole = await _dbContext.UserRoles
            .FirstOrDefaultAsync(ur => ur.Id == id, cancellationToken);

        if (userRole is null)
        {
            return null;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        return userRole;
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var userRole = await _dbContext.UserRoles
            .Include(ur => ur.User)
            .FirstOrDefaultAsync(ur => ur.Id == id, cancellationToken);

        if (userRole is null)
        {
            return false;
        }

        // Check if this is the last role for the user
        var userRoleCount = await _dbContext.UserRoles
            .CountAsync(ur => ur.UserId == userRole.UserId, cancellationToken);

        if (userRoleCount <= 1)
        {
            throw new InvalidOperationException("Cannot remove the last role from a user");
        }

        _dbContext.UserRoles.Remove(userRole);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task AssignMultipleRolesAsync(int userId, List<int> roleIds, int assignedBy, DateTime? expiredAt = null, CancellationToken cancellationToken = default)
    {
        // Check if user exists
        var user = await _dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == userId && !u.IsDeleted, cancellationToken);
        if (user is null)
        {
            throw new KeyNotFoundException($"User with ID {userId} not found");
        }

        // Get existing role assignments
        var existingRoleIds = await _dbContext.UserRoles
            .Where(ur => ur.UserId == userId)
            .Select(ur => ur.RoleId)
            .ToListAsync(cancellationToken);

        // Filter out already assigned roles
        var newRoleIds = roleIds.Except(existingRoleIds).ToList();

        foreach (var roleId in newRoleIds)
        {
            // Check if role exists
            var role = await _dbContext.Roles
                .AsNoTracking()
                .FirstOrDefaultAsync(r => r.Id == roleId, cancellationToken);
            if (role is null)
            {
                throw new KeyNotFoundException($"Role with ID {roleId} not found");
            }

            var userRole = new UserRoles
            {
                UserId = userId,
                RoleId = roleId,
                AssignedAt = DateTime.UtcNow,
                AssignedBy = assignedBy,
            };

            _dbContext.UserRoles.Add(userRole);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task RemoveMultipleRolesAsync(int userId, List<int> roleIds, CancellationToken cancellationToken = default)
    {
        // Check if user exists
        var user = await _dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == userId && !u.IsDeleted, cancellationToken);
        if (user is null)
        {
            throw new KeyNotFoundException($"User with ID {userId} not found");
        }

        // Get total role count
        var totalRoleCount = await _dbContext.UserRoles
            .CountAsync(ur => ur.UserId == userId, cancellationToken);

        // Get count of roles to be removed
        var rolesToRemove = await _dbContext.UserRoles
            .Where(ur => ur.UserId == userId && roleIds.Contains(ur.RoleId))
            .ToListAsync(cancellationToken);

        // Check if removing would leave user with no roles
        if (totalRoleCount <= rolesToRemove.Count)
        {
            throw new InvalidOperationException("Cannot remove the last role from a user");
        }

        _dbContext.UserRoles.RemoveRange(rolesToRemove);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
