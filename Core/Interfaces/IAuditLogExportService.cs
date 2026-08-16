using Hgs.Share.Requests.Audit;

namespace Core.Interfaces;

public interface IAuditLogExportService
{
    Task<byte[]> ExportCsvAsync(AuditLogsFilterRequest request, CancellationToken cancellationToken = default);
    Task<byte[]> ExportExcelAsync(AuditLogsFilterRequest request, CancellationToken cancellationToken = default);
}