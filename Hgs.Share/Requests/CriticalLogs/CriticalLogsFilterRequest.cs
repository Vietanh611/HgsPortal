namespace Hgs.Share.Requests.CriticalLogs;

/// <summary>
/// Bộ lọc truy vấn nhật ký lỗi hệ thống (bảng CriticalLogs do Serilog ghi).
/// Level/Keyword là chuỗi tự do khi bind query — giá trị không khớp sẽ trả 0 kết quả.
/// </summary>
public class CriticalLogsFilterRequest
{
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;

    /// <summary>Mức độ log: "Warning" | "Error" | "Fatal" (403/429 được ghi Warning; để trống = tất cả).</summary>
    public string? Level { get; set; }

    /// <summary>Tìm kiếm trong Message/MessageTemplate/Exception/Properties.</summary>
    public string? Keyword { get; set; }

    public DateTime? FromDate { get; set; }

    public DateTime? ToDate { get; set; }
}
