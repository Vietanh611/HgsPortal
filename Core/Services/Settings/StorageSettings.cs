namespace Core.Services.Settings;

public class StorageSettings
{
    public string AvatarDirectory { get; set; } = "uploads/avatars";

    public long MaxAvatarBytes { get; set; } = 2 * 1024 * 1024;

    public string[] AllowedAvatarExtensions { get; set; } = { ".jpg", ".jpeg", ".png", ".webp", ".gif" };
}