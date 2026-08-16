namespace Core.Services.Settings;

public class MailSettings
{
    public string Host { get; set; } = "smtp.office365.com";
    public int Port { get; set; } = 587;
    public string TenantId { get; set; } = string.Empty;
    public string AppId { get; set; } = string.Empty;
    public string AppSecret { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string From { get; set; } = string.Empty;
    public string FromName { get; set; } = "HGS Portal";
    public string ResetPasswordBaseUrl { get; set; } = "https://portal.hgs.vn/reset-password";
}