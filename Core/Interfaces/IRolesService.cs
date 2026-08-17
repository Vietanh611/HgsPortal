using Domain.Entities.Identity;

namespace Core.Interfaces;

public interface IRolesService
{
    Task<IEnumerable<Roles>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<Roles?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<Roles> CreateAsync(Roles request, CancellationToken cancellationToken = default);
    /// <summary>Cập nhật role; nếu role là role hệ thống (IsSystemRole), sự kiện được ghi là bảo mật Critical (SYSTEM_ROLE_MODIFIED) thay vì audit CRUD thường.</summary>
    Task<Roles?> UpdateAsync(int id, Roles request, CancellationToken cancellationToken = default);
    /// <summary>Từ chối xóa role đang được gán cho user (bảng UserRoles) — bảo toàn toàn vẹn phân quyền.</summary>
    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);
}
