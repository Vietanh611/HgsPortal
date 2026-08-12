using Domain.Entities.CoreAssets;
using Hgs.Share.Requests.CoreAssets;

namespace Core.Interfaces;

public interface ICoreAssetsService
{
    Task<IEnumerable<CoreAssets>> GetCoreAssetsAsync(CancellationToken cancellationToken = default);
    Task<CoreAssets?> GetCoreAssetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<CoreAssets> CreateCoreAssetAsync(CoreAssetsCreateRequest request, CancellationToken cancellationToken = default);
}