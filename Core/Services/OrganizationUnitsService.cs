using Core.Interfaces;
using Domain.Entities.Identity;
using Hgs.Share.Requests.OrganizationUnits;
using Microsoft.EntityFrameworkCore;

namespace Core.Services;

public class OrganizationUnitsService : IOrganizationUnitsService
{
    private readonly IAppDbContext _dbContext;

    public OrganizationUnitsService(IAppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IEnumerable<OrganizationUnits>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.OrganizationUnits
            .Include(x => x.Parent)
            .AsNoTracking()
            .OrderBy(x => x.SortOrder)
            .ToListAsync(cancellationToken);
    }

    public async Task<OrganizationUnits?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.OrganizationUnits
            .Include(x => x.Parent)
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<OrganizationUnits> CreateAsync(OrganizationUnitsCreateRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Code) || string.IsNullOrWhiteSpace(request.Name))
        {
            throw new ArgumentException("Code and name are required");
        }

        var exists = await _dbContext.OrganizationUnits
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Code == request.Code.Trim(), cancellationToken);
        if (exists is not null)
        {
            throw new InvalidOperationException("Organization unit code already exists");
        }

        var parent = request.ParentId is null
            ? null
            : await _dbContext.OrganizationUnits
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == request.ParentId.Value, cancellationToken);

        if (request.ParentId.HasValue && parent is null)
        {
            throw new KeyNotFoundException("Parent organization unit not found");
        }

        var organizationUnit = new OrganizationUnits
        {
            Code = request.Code.Trim(),
            Name = request.Name.Trim(),
            ParentId = request.ParentId,
            SortOrder = request.SortOrder,
            IsActive = request.IsActive,
            CreatedAt = DateTime.UtcNow,
            Level = parent?.Level + 1 ?? 0,
            Path = null
        };

        _dbContext.OrganizationUnits.Add(organizationUnit);
        await _dbContext.SaveChangesAsync(cancellationToken);

        if (parent is not null)
        {
            organizationUnit.Path = string.IsNullOrWhiteSpace(parent.Path)
                ? organizationUnit.Id.ToString()
                : $"{parent.Path}/{organizationUnit.Id}";
        }
        else
        {
            organizationUnit.Path = organizationUnit.Id.ToString();
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        return organizationUnit;
    }

    public async Task<OrganizationUnits?> UpdateAsync(int id, OrganizationUnitsUpdateRequest request, CancellationToken cancellationToken = default)
    {
        var organizationUnit = await _dbContext.OrganizationUnits
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (organizationUnit is null)
        {
            return null;
        }

        if (request.ParentId.HasValue && request.ParentId.Value == id)
        {
            throw new ArgumentException("Organization unit cannot be its own parent");
        }

        if (request.ParentId.HasValue)
        {
            var parent = await _dbContext.OrganizationUnits
                .FirstOrDefaultAsync(x => x.Id == request.ParentId.Value, cancellationToken);
            if (parent is null)
            {
                throw new KeyNotFoundException("Parent organization unit not found");
            }

            organizationUnit.ParentId = request.ParentId.Value;
            organizationUnit.Level = parent.Level + 1;
        }

        if (!string.IsNullOrWhiteSpace(request.Code))
        {
            var codeExists = await _dbContext.OrganizationUnits
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Code == request.Code.Trim(), cancellationToken);
            if (codeExists is not null && codeExists.Id != id)
            {
                throw new InvalidOperationException("Organization unit code already exists");
            }

            organizationUnit.Code = request.Code.Trim();
        }

        if (!string.IsNullOrWhiteSpace(request.Name))
        {
            organizationUnit.Name = request.Name.Trim();
        }

        if (request.SortOrder.HasValue)
        {
            organizationUnit.SortOrder = request.SortOrder.Value;
        }

        if (request.IsActive.HasValue)
        {
            organizationUnit.IsActive = request.IsActive.Value;
        }

        organizationUnit.UpdatedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);
        return organizationUnit;
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var organizationUnit = await _dbContext.OrganizationUnits
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (organizationUnit is null)
        {
            return false;
        }

        var hasUsers = await _dbContext.Users.AnyAsync(x => x.OrganizationUnitId == id && !x.IsDeleted, cancellationToken);
        if (hasUsers)
        {
            throw new InvalidOperationException("Cannot delete organization unit because it is used by users");
        }

        var hasRoles = await _dbContext.Roles.AnyAsync(x => x.OrganizationUnitId == id, cancellationToken);
        if (hasRoles)
        {
            throw new InvalidOperationException("Cannot delete organization unit because it is used by roles");
        }

        var hasChildren = await _dbContext.OrganizationUnits.AnyAsync(x => x.ParentId == id, cancellationToken);
        if (hasChildren)
        {
            throw new InvalidOperationException("Cannot delete organization unit because it has child units");
        }

        _dbContext.OrganizationUnits.Remove(organizationUnit);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }
}
