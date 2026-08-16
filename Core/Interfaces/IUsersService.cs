using Domain.Entities.FlyOps;
using Domain.Entities.Identity;
using Hgs.Share.Requests.Users;

namespace Core.Interfaces;

public interface IUsersService
{
    Task<IEnumerable<Users>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<NhanVien>> GetAllBravoNhanVienAsync(CancellationToken cancellationToken = default);
    Task<Users?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<Users> CreateAsync(UsersCreateRequest request, CancellationToken cancellationToken = default);
    Task<Users?> UpdateAsync(int id, UsersUpdateRequest request, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);
    Task<bool> ChangePasswordAsync(int id, UsersChangePasswordRequest request, CancellationToken cancellationToken = default);
    Task<bool> ResetPasswordAsync(int id, UsersResetPasswordRequest request, CancellationToken cancellationToken = default);
    Task<string?> UploadAvatarAsync(int id, Stream fileStream, string fileName, string contentType, string avatarDirectory, CancellationToken cancellationToken = default);
}
