namespace Domain.Entities.System;

/// <summary>
/// Bản ghi log lỗi do Serilog MSSqlServer sink ghi trực tiếp vào
/// bảng <c>dbo.CriticalLogs</c> (chỉ nhận Error/Fatal). Không nằm trong
/// mô hình audit nghiệp vụ (<c>AuditLogs</c>) — đây là log hạ tầng để
/// admin giám sát/kiểm soát lỗi hệ thống. Bảng do script
/// <c>Scripts/CreateCriticalLogsTable.sql</c> quản lý, EF Core chỉ đọc.
/// </summary>
public class CriticalLogs
{
    public long Id { get; set; }

    public string? Message { get; set; }

    public string? MessageTemplate { get; set; }

    public string? Level { get; set; }

    public DateTime TimeStamp { get; set; }

    public string? Exception { get; set; }

    /// <summary>Toàn bộ structured properties của log event (XML do sink ghi).</summary>
    public string? Properties { get; set; }

    /// <summary>Trace identifier của request, điền từ LogContext — dùng đối chiếu request log.</summary>
    public string? RequestId { get; set; }

    /// <summary>Tên đăng nhập người gây ra lỗi (nếu request có xác thực), điền từ LogContext.</summary>
    public string? User { get; set; }

    public string? Path { get; set; }

    public string? Method { get; set; }
}
