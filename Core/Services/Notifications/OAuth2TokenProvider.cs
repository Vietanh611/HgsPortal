using Microsoft.Identity.Client;
using Microsoft.Extensions.Options;
using Core.Services.Settings;

namespace Core.Services.Notifications;

public class OAuth2TokenProvider
{
    private readonly MailSettings _mailSettings;
    private readonly object _lock = new();
    private string? _cachedToken;
    private DateTimeOffset _tokenExpiresAt;

    public OAuth2TokenProvider(IOptions<MailSettings> mailSettings)
    {
        _mailSettings = mailSettings.Value;
    }

    public async Task<string> GetAccessTokenAsync(CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            if (_cachedToken is not null && _tokenExpiresAt > DateTimeOffset.UtcNow.AddMinutes(5))
            {
                return _cachedToken;
            }
        }

        var app = ConfidentialClientApplicationBuilder
            .Create(_mailSettings.AppId)
            .WithClientSecret(_mailSettings.AppSecret)
            .WithAuthority(new Uri($"https://login.microsoftonline.com/{_mailSettings.TenantId}"))
            .Build();

        var result = await ((IByUsernameAndPassword)app)
            .AcquireTokenByUsernamePassword(
                new[] { "https://outlook.office.com/SMTP.Send", "offline_access" },
                _mailSettings.Username,
                _mailSettings.Password)
            .ExecuteAsync(cancellationToken);

        lock (_lock)
        {
            _cachedToken = result.AccessToken;
            _tokenExpiresAt = result.ExpiresOn;
        }

        return result.AccessToken;
    }
}