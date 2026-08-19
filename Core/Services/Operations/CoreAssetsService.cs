using Core.Interfaces.Operations;
using Data.DbContexts;
using Domain.Entities.CoreAssets;
using Hgs.Share.Exceptions;
using Hgs.Share.Requests.CoreAssets;
using Microsoft.EntityFrameworkCore;

namespace Core.Services.Operations;

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

    public async Task<CoreAssets> UpdateCoreAssetAsync(int id, CoreAssetsUpdateRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Code) || string.IsNullOrWhiteSpace(request.StoragePath) || string.IsNullOrWhiteSpace(request.AssetType))
        {
            throw new ArgumentException("Code, StoragePath and AssetType are required");
        }

        var asset = await _dbContext.CoreAssets
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (asset is null)
        {
            throw new NotFoundException("Core asset not found");
        }

        var exists = await _dbContext.CoreAssets
            .AnyAsync(x => x.Code == request.Code.Trim() && x.Id != id, cancellationToken);

        if (exists)
        {
            throw new InvalidOperationException("Asset code already exists");
        }

        asset.Code = request.Code.Trim();
        asset.FileName = request.FileName?.Trim();
        asset.ContentType = request.ContentType?.Trim();
        asset.StoragePath = request.StoragePath.Trim();
        asset.AssetType = request.AssetType.Trim();
        asset.IsActive = request.IsActive;

        await _dbContext.SaveChangesAsync(cancellationToken);

        return asset;
    }

    public async Task DeleteCoreAssetAsync(int id, string contentRootPath, CancellationToken cancellationToken = default)
    {
        var asset = await _dbContext.CoreAssets
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (asset is null)
        {
            throw new NotFoundException("Core asset not found");
        }

        var storagePath = asset.StoragePath;
        _dbContext.CoreAssets.Remove(asset);
        await _dbContext.SaveChangesAsync(cancellationToken);

        DeletePhysicalFileBestEffort(storagePath, contentRootPath);
    }

    private static void DeletePhysicalFileBestEffort(string? storagePath, string contentRootPath)
    {
        // Best-effort: chỉ xóa file khi StoragePath là đường dẫn tương đối trỏ vào file nằm
        // trong thư mục server API (content root). URL tuyệt đối hoặc đường dẫn ngoài phạm vi
        // (ví dụ file thuộc wwwroot của WebApp) được bỏ qua — không báo lỗi vì file có thể
        // được dùng chung hoặc nằm trên server khác.
        if (string.IsNullOrWhiteSpace(storagePath))
        {
            return;
        }

        if (Uri.TryCreate(storagePath, UriKind.Absolute, out _))
        {
            return;
        }

        try
        {
            var root = Path.GetFullPath(contentRootPath);
            var physicalPath = Path.GetFullPath(Path.Combine(root, storagePath.TrimStart('/', '\\')));

            // Chống path traversal: chỉ xóa file khi nằm trong content root.
            if (!physicalPath.StartsWith(root, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            if (File.Exists(physicalPath))
            {
                File.Delete(physicalPath);
            }
        }
        catch (Exception ex)
        {
            // Best-effort: lỗi filesystem không làm lỗi thao tác xóa bản ghi.
            Console.WriteLine($"Could not delete core asset file '{storagePath}': {ex.Message}");
        }
    }
}