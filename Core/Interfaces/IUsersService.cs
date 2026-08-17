using Domain.Entities.FlyOps;
using Domain.Entities.Identity;
using Hgs.Share.Requests.Users;

namespace Core.Interfaces;

public interface IUsersService
{
    /// <summary>Trả về người dùng trong phạm vi tổ chức của caller (lọc theo Path của OrganizationUnit), loại trừ user đã xóa mềm; SUPER_ADMIN nhận toàn bộ.</summary>
    Task<IEnumerable<Users>> GetAllAsync(CancellationToken cancellationToken = default);
    /// <summary>Đọc danh sách nhân viên chưa nghỉ việc từ DB Bravo (FlyOps) — nguồn nhân sự ngoài hệ thống HGS, không phải bảng Users.</summary>
    Task<IEnumerable<NhanVien>> GetAllBravoNhanVienAsync(CancellationToken cancellationToken = default);
    /// <summary>Lấy người dùng theo Id (loại user đã xóa mềm). Trả null nếu user ngoài phạm vi tổ chức của caller — không tiết lộ sự tồn tại của bản ghi ngoài quyền.</summary>
    Task<Users?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    /// <summary>Tạo người dùng mới: kiểm tra username duy nhất và OrganizationUnitId phải thuộc phạm vi tổ chức của caller — chống tạo tài khoản ngoài quyền quản lý.</summary>
    Task<Users> CreateAsync(UsersCreateRequest request, CancellationToken cancellationToken = default);
    /// <summary>Cập nhật từng phần thông tin người dùng trong phạm vi tổ chức của caller; không cho chuyển user sang org unit ngoài phạm vi.</summary>
    Task<Users?> UpdateAsync(int id, UsersUpdateRequest request, CancellationToken cancellationToken = default);
    /// <summary>Xóa mềm người dùng (đặt IsDeleted=true, giữ bản ghi) thay vì xóa vật lý để bảo toàn lịch sử audit và ràng buộc tham chiếu.</summary>
    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);
    /// <summary>Đổi mật khẩu sau khi xác minh mật khẩu hiện tại — người không biết mật khẩu cũ không thể thay đổi (khác ResetPasswordAsync dành cho quản trị).</summary>
    Task<bool> ChangePasswordAsync(int id, UsersChangePasswordRequest request, CancellationToken cancellationToken = default);
    /// <summary>Đặt lại mật khẩu không cần mật khẩu hiện tại (thao tác quản trị); bắt buộc user thuộc phạm vi tổ chức của caller.</summary>
    Task<bool> ResetPasswordAsync(int id, UsersResetPasswordRequest request, CancellationToken cancellationToken = default);
    /// <summary>Tải ảnh đại diện: giới hạn định dạng (AllowedAvatarExtensions) và dung lượng (MaxAvatarBytes) theo cấu hình Storage; sinh tên file duy nhất và xóa file ảnh cũ nếu có. User phải thuộc phạm vi caller.</summary>
    Task<string?> UploadAvatarAsync(int id, Stream fileStream, string fileName, string contentType, string avatarDirectory, CancellationToken cancellationToken = default);
}
