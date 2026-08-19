namespace Hgs.Share.Requests.CoreAssets;

public class CoreAssetsUpdateRequest
{
    public string Code { get; set; } = string.Empty;
    public string? FileName { get; set; }
    public string? ContentType { get; set; }
    public string StoragePath { get; set; } = string.Empty;
    public string AssetType { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}
