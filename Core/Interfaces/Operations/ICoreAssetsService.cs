using Domain.Entities.CoreAssets;
using Hgs.Share.Requests.CoreAssets;

namespace Core.Interfaces.Operations;

public interface ICoreAssetsService
{
    Task<IEnumerable<CoreAssets>> GetCoreAssetsAsync(CancellationToken cancellationToken = default);
    Task<CoreAssets?> GetCoreAssetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<CoreAssets> CreateCoreAssetAsync(CoreAssetsCreateRequest request, CancellationToken cancellationToken = default);
    Task<CoreAssets> UpdateCoreAssetAsync(int id, CoreAssetsUpdateRequest request, CancellationToken cancellationToken = default);
    Task DeleteCoreAssetAsync(int id, string contentRootPath, CancellationToken cancellationToken = default);
}