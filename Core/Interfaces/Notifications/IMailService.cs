namespace Core.Interfaces.Notifications;

public interface IMailService
{
    /// <summary>
    /// Gửi email qua SMTP (OAuth2). Nếu SMTP chưa được cấu hình (thiếu Host/From) thì bỏ qua gửi kèm log
    /// warning thay vì ném lỗi, để môi trường không có SMTP (ví dụ dev) không làm hỏng luồng nghiệp vụ.
    /// </summary>
    Task SendAsync(Services.Notifications.MailMessage message, CancellationToken cancellationToken = default);
}