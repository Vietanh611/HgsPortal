using Hgs.Share.Requests.Audit;

namespace Core.Interfaces.Operations;

public interface IAuditLogExportService
{
    /// <summary>
    /// Xuất nhật ký audit sang file CSV (kèm BOM cho Excel tiếng Việt).
    /// Khoảng thời gian bị giới hạn tối đa 90 ngày (mặc định 30 ngày gần nhất)
    /// và dữ liệu bị giới hạn 50.000 dòng để tránh export quá tải.
    /// Mỗi lần export tự ghi một sự kiện audit EXPORT — dữ liệu audit là nhạy cảm.
    /// </summary>
    Task<byte[]> ExportCsvAsync(AuditLogsFilterRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Xuất nhật ký audit sang file Excel (xlsx). Áp dụng cùng giới hạn
    /// 90 ngày / 50.000 dòng và tự ghi sự kiện audit EXPORT như
    /// <see cref="ExportCsvAsync"/>.
    /// </summary>
    Task<byte[]> ExportExcelAsync(AuditLogsFilterRequest request, CancellationToken cancellationToken = default);
}