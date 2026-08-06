namespace Hgs.Share.Attributes
{
    /// <summary>
    /// Đánh dấu property KHÔNG được đưa vào AuditLogs (OldValue/NewValue),
    /// nhưng KHÔNG ảnh hưởng gì tới JSON trả về từ API — vì ASP.NET Core
    /// dùng JsonSerializerOptions mặc định, không biết tới attribute này.
    /// Dùng cho: navigation property (tránh vòng lặp/log rác) và
    /// cột nhạy cảm (VD: PasswordHash).
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class AuditIgnoreAttribute : Attribute
    {
    }
}
