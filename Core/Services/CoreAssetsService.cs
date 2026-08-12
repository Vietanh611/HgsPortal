using Core.Interfaces;
using Data.DbContexts;
using Domain.Entities.CoreAssets;
using Hgs.Share.Requests.CoreAssets;
using Microsoft.EntityFrameworkCore;

namespace Core.Services;

public class CoreAssetsService : ICoreAssetsService
{
    private readonly HgsDbContext _dbContext;

    public CoreAssetsService(HgsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IEnumerable<CoreAssets>> GetCoreAssetsAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.CoreAssets
            .AsNoTracking()
            .OrderBy(x => x.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task<CoreAssets?> GetCoreAssetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.CoreAssets
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<CoreAssets> CreateCoreAssetAsync(CoreAssetsCreateRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Code) || string.IsNullOrWhiteSpace(request.StoragePath))
        {
            throw new ArgumentException("Code and StoragePath are required");
        }

        var exists = await _dbContext.CoreAssets
            .AnyAsync(x => x.Code == request.Code.Trim(), cancellationToken);

        if (exists)
        {
            throw new InvalidOperationException("Asset code already exists");
        }

        var asset = new CoreAssets
        {
            Code = request.Code.Trim(),
            FileName = request.FileName?.Trim(),
            ContentType = request.ContentType?.Trim(),
            StoragePath = request.StoragePath.Trim(),
            AssetType = request.AssetType.Trim(),
            IsActive = request.IsActive
        };

        _dbContext.CoreAssets.Add(asset);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return asset;
    }
}