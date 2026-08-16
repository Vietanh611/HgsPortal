namespace Core.Interfaces;

public interface IMailService
{
    Task SendAsync(Core.Services.Notifications.MailMessage message, CancellationToken cancellationToken = default);
}