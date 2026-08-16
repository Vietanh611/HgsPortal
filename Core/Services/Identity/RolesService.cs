using Core.Interfaces;
using Data.DbContexts;
using Domain.Entities.Identity;
using Microsoft.EntityFrameworkCore;

namespace Core.Services.Identity;

public class RolesService : IRolesService
{
    private readonly HgsDbContext _dbContext;
    private readonly IAuditLogService _auditLog;

    public RolesService(HgsDbContext dbContext, IAuditLogService auditLog)
    {
        _dbContext = dbContext;
        _auditLog = auditLog;
    }

    public async Task<IEnumerable<Roles>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.Roles
            .Include(x => x.OrganizationUnit)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<Roles?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Roles
            .Include(x => x.OrganizationUnit)
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<Roles> CreateAsync(Roles request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Code) || string.IsNullOrWhiteSpace(request.Name))
        {
            throw new ArgumentException("Code and name are required");
        }

        var exists = await _dbContext.Roles
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Code == request.Code.Trim(), cancellationToken);
        if (exists is not null)
        {
            throw new InvalidOperationException("Role code already exists");
        }

        _dbContext.Roles.Add(request);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _auditLog.Log(
            action: "CREATE",
            entityName: "Roles",
            entityId: request.Id,
            oldValue: null,
            newValue: request);

        await _dbContext.SaveChangesAsync(cancellationToken);
        return request;
    }

    public async Task<Roles?> UpdateAsync(int id, Roles request, CancellationToken cancellationToken = default)
    {
        var role = await _dbContext.Roles
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (role is null)
        {
            return null;
        }

        var oldSnapshot = new
        {
            role.Id,
            role.Code,
            role.Name,
            role.Description,
            role.OrganizationUnitId,
            role.DataScope,
            role.IsSystemRole,
            role.IsActive
        };

        if (!string.IsNullOrWhiteSpace(request.Code))
        {
            var exists = await _dbContext.Roles
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Code == request.Code.Trim(), cancellationToken);
            if (exists is not null && exists.Id != id)
            {
                throw new InvalidOperationException("Role code already exists");
            }

            role.Code = request.Code.Trim();
        }

        if (!string.IsNullOrWhiteSpace(request.Name))
        {
            role.Name = request.Name.Trim();
        }

        if (request.Description is not null)
        {
            role.Description = request.Description;
        }

        if (request.OrganizationUnitId.HasValue)
        {
            role.OrganizationUnitId = request.OrganizationUnitId;
        }

        if (request.DataScope is not null)
        {
            role.DataScope = request.DataScope;
        }

        if (request.IsSystemRole)
        {
            role.IsSystemRole = request.IsSystemRole;
        }

        if (request.IsActive)
        {
            role.IsActive = request.IsActive;
        }

        _auditLog.Log(
            action: "UPDATE",
            entityName: "Roles",
            entityId: role.Id,
            oldValue: oldSnapshot,
            newValue: role);

        await _dbContext.SaveChangesAsync(cancellationToken);
        return role;
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var role = await _dbContext.Roles
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (role is null)
        {
            return false;
        }

        var hasAssignments = await _dbContext.UserRoles.AnyAsync(x => x.RoleId == id, cancellationToken);
        if (hasAssignments)
        {
            throw new InvalidOperationException("Cannot delete role because it is assigned to users");
        }

        _auditLog.Log(
            action: "DELETE",
            entityName: "Roles",
            entityId: role.Id,
            oldValue: role,
            newValue: null);

        _dbContext.Roles.Remove(role);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }
}
