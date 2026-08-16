using Core.Interfaces;
using Core.Services.Settings;
using Hgs.Share.Exceptions;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;

namespace Core.Services.Notifications;

public class MailService : IMailService
{
    private readonly MailSettings _mailSettings;
    private readonly OAuth2TokenProvider _tokenProvider;
    private readonly ILogger<MailService> _logger;

    public MailService(
        IOptions<MailSettings> mailSettings,
        OAuth2TokenProvider tokenProvider,
        ILogger<MailService> logger)
    {
        _mailSettings = mailSettings.Value;
        _tokenProvider = tokenProvider;
        _logger = logger;
    }

    public async Task SendAsync(MailMessage message, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_mailSettings.Host) ||
            string.IsNullOrWhiteSpace(_mailSettings.From))
        {
            _logger.LogWarning("MailSettings not configured; skipping send to '{To}'.", message.To);
            return;
        }

        using var smtp = new SmtpClient();
        try
        {
            var accessToken = await _tokenProvider.GetAccessTokenAsync(cancellationToken);
            await smtp.ConnectAsync(_mailSettings.Host, _mailSettings.Port, SecureSocketOptions.StartTls, cancellationToken);
            await smtp.AuthenticateAsync(new SaslMechanismOAuth2(_mailSettings.Username, accessToken), cancellationToken);

            var mime = new MimeMessage
            {
                From = { MailboxAddress.Parse($"{_mailSettings.FromName} <{_mailSettings.From}>") },
                To = { MailboxAddress.Parse(message.To) },
                Subject = message.Subject,
                Body = new BodyBuilder { HtmlBody = message.HtmlBody }.ToMessageBody()
            };

            await smtp.SendAsync(mime, cancellationToken);
            _logger.LogInformation("Email sent to '{To}' with subject '{Subject}'.", message.To, message.Subject);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to '{To}'.", message.To);
            throw new BadRequestException("Unable to send email. Please try again later.");
        }
        finally
        {
            if (smtp.IsConnected)
            {
                await smtp.DisconnectAsync(true, cancellationToken);
            }
        }
    }
}