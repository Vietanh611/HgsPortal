using Domain.Entities.Identity;
using Hgs.Share.Requests.OrganizationUnits;

namespace Core.Interfaces;

public interface IOrganizationUnitsService
{
    Task<IEnumerable<OrganizationUnits>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<OrganizationUnits?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<OrganizationUnits> CreateAsync(OrganizationUnitsCreateRequest request, CancellationToken cancellationToken = default);
    Task<OrganizationUnits?> UpdateAsync(int id, OrganizationUnitsUpdateRequest request, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);
}
