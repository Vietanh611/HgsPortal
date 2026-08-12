using Core.Interfaces;
using Domain.Entities.CoreAssets;
using Hgs.Share.Requests.CoreAssets;
using Hgs.Share.Responses.ApiResponses;
using Hgs.Share.Responses.CoreAssets;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CoreAssetsController : ControllerBase
{
    private readonly ICoreAssetsService _coreAssetsService;
    private readonly ILogger<CoreAssetsController> _logger;

    public CoreAssetsController(ICoreAssetsService coreAssetsService, ILogger<CoreAssetsController> logger)
    {
        _coreAssetsService = coreAssetsService;
        _logger = logger;
    }

    [HttpGet]
    [AllowAnonymous]
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