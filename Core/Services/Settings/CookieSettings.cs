using Microsoft.AspNetCore.Http;

namespace Core.Services.Settings;

public class CookieSettings
{
    public bool Secure { get; set; } = true;
    public SameSiteMode SameSite { get; set; } = SameSiteMode.None;
}
