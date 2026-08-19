using API.Authorization;
using Core.Interfaces.Operations;
using Domain.Entities.CoreAssets;
using Hgs.Share.Exceptions;
using Hgs.Share.Requests.CoreAssets;
using Hgs.Share.Responses.ApiResponses;
using Hgs.Share.Responses.CoreAssets;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace API.Controllers.System;

[ApiController]
[Route("api/[controller]")]
[MenuPermission("COREASSETS")]
[EnableRateLimiting("device")]
public class CoreAssetsController : ControllerBase
{
    private readonly ICoreAssetsService _coreAssetsService;
    private readonly IWebHostEnvironment _env;
    private readonly ILogger<CoreAssetsController> _logger;

    public CoreAssetsController(ICoreAssetsService coreAssetsService, IWebHostEnvironment env, ILogger<CoreAssetsController> logger)
    {
        _coreAssetsService = coreAssetsService;
        _env = env;
        _logger = logger;
    }

    /// <summary>
    /// Danh sách core assets; chấp nhận xác thực bằng DeviceKey hoặc JWT (kiosk hoặc user WebApp).
    /// </summary>
    [HttpGet]
    [Authorize(AuthenticationSchemes = "DeviceKey,Bearer")]
    public async Task<ActionResult<ApiResponse<IEnumerable<CoreAssetsGetAllResponse>>>> GetCoreAssets(CancellationToken cancellationToken)
    {
        var assets = await _coreAssetsService.GetCoreAssetsAsync(cancellationToken);
        var response = assets.Select(MapToGetAllResponse).ToList();
        return Ok(ApiResponse<IEnumerable<CoreAssetsGetAllResponse>>.SuccessResponse(response, "Core assets retrieved successfully", 200));
    }

    [HttpGet("{id:int}")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<CoreAssetsGetByIdResponse>>> GetCoreAssetById(int id, CancellationToken cancellationToken)
    {
        var asset = await _coreAssetsService.GetCoreAssetByIdAsync(id, cancellationToken);
        if (asset is null)
        {
            return NotFound(ApiResponse<CoreAssetsGetByIdResponse>.FailResponse("Core asset not found", 404));
        }

        return Ok(ApiResponse<CoreAssetsGetByIdResponse>.SuccessResponse(MapToGetByIdResponse(asset), "Core asset retrieved successfully", 200));
    }

    [HttpPost]
    [Authorize]
    public async Task<ActionResult<ApiResponse<CoreAssetsCreateResponse>>> CreateCoreAsset([FromBody] CoreAssetsCreateRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var asset = await _coreAssetsService.CreateCoreAssetAsync(request, cancellationToken);
            _logger.LogInformation("Created core asset '{Code}'.", asset.Code);
            return Ok(ApiResponse<CoreAssetsCreateResponse>.SuccessResponse(MapToCreateResponse(asset), "Core asset created successfully", 201));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ApiResponse<CoreAssetsCreateResponse>.FailResponse(ex.Message, 400));
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(ApiResponse<CoreAssetsCreateResponse>.FailResponse(ex.Message, 409));
        }
    }

    [HttpPut("{id:int}")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<CoreAssetsCreateResponse>>> UpdateCoreAsset(int id, [FromBody] CoreAssetsUpdateRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var asset = await _coreAssetsService.UpdateCoreAssetAsync(id, request, cancellationToken);
            _logger.LogInformation("Updated core asset '{Code}'.", asset.Code);
            return Ok(ApiResponse<CoreAssetsCreateResponse>.SuccessResponse(MapToCreateResponse(asset), "Core asset updated successfully", 200));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ApiResponse<CoreAssetsCreateResponse>.FailResponse(ex.Message, 400));
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(ApiResponse<CoreAssetsCreateResponse>.FailResponse(ex.Message, 409));
        }
        catch (NotFoundException ex)
        {
            return NotFound(ApiResponse<CoreAssetsCreateResponse>.FailResponse(ex.Message, 404));
        }
    }

    /// <summary>
    /// Xóa vĩnh viễn core asset; cố xóa file vật lý theo StoragePath nếu file nằm trong
    /// thư mục server API (best-effort, không thấy thì bỏ qua).
    /// </summary>
    [HttpDelete("{id:int}")]
    [Authorize]
    public async Task<ActionResult<ApiResponse>> DeleteCoreAsset(int id, CancellationToken cancellationToken)
    {
        try
        {
            await _coreAssetsService.DeleteCoreAssetAsync(id, _env.ContentRootPath, cancellationToken);
            _logger.LogInformation("Deleted core asset '{Id}'.", id);
            return Ok(ApiResponse.SuccessResponse("Core asset deleted successfully", 200));
        }
        catch (NotFoundException ex)
        {
            return NotFound(ApiResponse.FailResponse(ex.Message, 404));
        }
    }

    private static CoreAssetsGetAllResponse MapToGetAllResponse(CoreAssets asset) => new()
    {
        Id = asset.Id,
        Code = asset.Code,
        FileName = asset.FileName,
        ContentType = asset.ContentType,
        StoragePath = asset.StoragePath,
        AssetType = asset.AssetType,
        IsActive = asset.IsActive
    };

    private static CoreAssetsGetByIdResponse MapToGetByIdResponse(CoreAssets asset) => new()
    {
        Id = asset.Id,
        Code = asset.Code,
        FileName = asset.FileName,
        ContentType = asset.ContentType,
        StoragePath = asset.StoragePath,
        AssetType = asset.AssetType,
        IsActive = asset.IsActive
    };

    private static CoreAssetsCreateResponse MapToCreateResponse(CoreAssets asset) => new()
    {
        Id = asset.Id,
        Code = asset.Code,
        FileName = asset.FileName,
        ContentType = asset.ContentType,
        StoragePath = asset.StoragePath,
        AssetType = asset.AssetType,
        IsActive = asset.IsActive
    };
}