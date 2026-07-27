using Domain.Entities.Identity;

namespace Core.Interfaces;

public interface IRolesService
{
    Task<IEnumerable<Roles>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<Roles?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<Roles> CreateAsync(Roles request, CancellationToken cancellationToken = default);
    Task<Roles?> UpdateAsync(int id, Roles request, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);
}
