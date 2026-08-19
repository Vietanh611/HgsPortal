using Domain.Entities.Identity;
using Hgs.Share.Requests.OrganizationUnits;

namespace Core.Interfaces.Identity;

public interface IOrganizationUnitsService
{
    Task<IEnumerable<OrganizationUnits>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<OrganizationUnits?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    /// <summary>Tạo org unit: Code duy nhất (trim). Level và Path được tự sinh — Path là chuỗi id cha→con, dùng làm cơ sở lọc phạm vi tổ chức (OrgScope).</summary>
    Task<OrganizationUnits> CreateAsync(OrganizationUnitsCreateRequest request, CancellationToken cancellationToken = default);
    /// <summary>Cập nhật org unit: không cho đặt chính nó làm cha, cha mới phải tồn tại; khi đổi cha, Level được tính lại từ cha mới.</summary>
    Task<OrganizationUnits?> UpdateAsync(int id, OrganizationUnitsUpdateRequest request, CancellationToken cancellationToken = default);
    /// <summary>Xóa org unit chỉ khi không còn được tham chiếu: user đang hoạt động, role, hay org unit con — bảo toàn toàn vẹn cây tổ chức và phạm vi.</summary>
    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);
}
