using ClosedXML.Excel;
using Core.Interfaces.Operations;
using Domain.Entities.Identity;
using Hgs.Share.Exceptions;
using Hgs.Share.Requests.Audit;
using System.Globalization;
using System.Text;

namespace Core.Services.Operations;

public class AuditLogExportService : IAuditLogExportService
{
    private const int MaxExportRows = 50_000;
    private const int MaxExportDays = 90;
    private const int DefaultExportDays = 30;
    private const int MaxDetailLength = 500;

    private readonly IAuditLogService _auditLogService;

    public AuditLogExportService(IAuditLogService auditLogService)
    {
        _auditLogService = auditLogService;
    }

    public async Task<byte[]> ExportCsvAsync(AuditLogsFilterRequest request, CancellationToken cancellationToken = default)
    {
        var rows = await GetExportRowsAsync(request, cancellationToken);

        var csv = new StringBuilder();
        csv.Append('\uFEFF'); // UTF-8 BOM — Excel mở CSV tiếng Việt không lỗi ký tự
        csv.AppendLine("Thời gian,Loại sự kiện,Hành động,Người thực hiện,Đối tượng bị tác động,Kết quả,Mức độ,Địa chỉ IP,Chi tiết thay đổi,Correlation ID");

        foreach (var row in rows)
        {
            csv.AppendLine(string.Join(',', new[]
            {
                EscapeCsv(FormatTime(row.CreatedAt)),
                EscapeCsv(row.EventCategory),
                EscapeCsv(row.Action),
                EscapeCsv(GetPerformerName(row)),
                EscapeCsv(GetTargetName(row)),
                EscapeCsv(row.Success ? "Thành công" : "Thất bại"),
                EscapeCsv(row.Severity),
                EscapeCsv(row.IpAddress ?? string.Empty),
                EscapeCsv(GetDetailSummary(row)),
                EscapeCsv(row.CorrelationId ?? string.Empty)
            }));
        }

        await LogExportEventAsync(request, rows.Count, "csv", cancellationToken);
        return Encoding.UTF8.GetBytes(csv.ToString());
    }

    public async Task<byte[]> ExportExcelAsync(AuditLogsFilterRequest request, CancellationToken cancellationToken = default)
    {
        var rows = await GetExportRowsAsync(request, cancellationToken);

        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("AuditLogs");

        var headers = new[] { "Thời gian", "Loại sự kiện", "Hành động", "Người thực hiện", "Đối tượng bị tác động", "Kết quả", "Mức độ", "Địa chỉ IP", "Chi tiết thay đổi", "Correlation ID" };
        for (var c = 0; c < headers.Length; c++)
        {
            worksheet.Cell(1, c + 1).Value = headers[c];
        }

        for (var r = 0; r < rows.Count; r++)
        {
            var row = rows[r];
            worksheet.Cell(r + 2, 1).Value = ToVnLocal(row.CreatedAt);
            worksheet.Cell(r + 2, 1).Style.DateFormat.Format = "dd/MM/yyyy HH:mm:ss";
            worksheet.Cell(r + 2, 2).Value = row.EventCategory;
            worksheet.Cell(r + 2, 3).Value = row.Action;
            worksheet.Cell(r + 2, 4).Value = GetPerformerName(row);
            worksheet.Cell(r + 2, 5).Value = GetTargetName(row);
            worksheet.Cell(r + 2, 6).Value = row.Success ? "Thành công" : "Thất bại";
            worksheet.Cell(r + 2, 7).Value = row.Severity;
            worksheet.Cell(r + 2, 8).Value = row.IpAddress ?? string.Empty;
            worksheet.Cell(r + 2, 9).Value = GetDetailSummary(row);
            worksheet.Cell(r + 2, 10).Value = row.CorrelationId ?? string.Empty;
        }

        worksheet.SheetView.FreezeRows(1);
        worksheet.Columns().AdjustToContents();

        await LogExportEventAsync(request, rows.Count, "xlsx", cancellationToken);

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    private async Task<List<AuditLogs>> GetExportRowsAsync(AuditLogsFilterRequest request, CancellationToken cancellationToken)
    {
        NormalizeAndValidateDateRange(request);

        var total = await _auditLogService.CountAsync(request, cancellationToken);
        if (total > MaxExportRows)
        {
            throw new BusinessRuleException($"Kết quả export ({total:N0} dòng) vượt quá giới hạn {MaxExportRows:N0} dòng. Vui lòng thu hẹp filter.");
        }

        // Cap cứng phòng race — dữ liệu có thể tăng giữa lúc count và query thật
        return await _auditLogService.GetAllFilteredAsync(request, cancellationToken);
    }

    private static void NormalizeAndValidateDateRange(AuditLogsFilterRequest request)
    {
        var now = DateTime.UtcNow;

        if (!request.FromDate.HasValue && !request.ToDate.HasValue)
        {
            // Mặc định 30 ngày gần nhất
            request.FromDate = now.AddDays(-DefaultExportDays);
            request.ToDate = now;
            return;
        }

        var from = request.FromDate ?? now.AddDays(-DefaultExportDays);
        var to = request.ToDate ?? now;

        if (to < from)
        {
            throw new BadRequestException("Khoảng thời gian không hợp lệ: ToDate phải >= FromDate");
        }

        if ((to - from).TotalDays > MaxExportDays)
        {
            throw new BadRequestException($"Khoảng thời gian export tối đa {MaxExportDays} ngày");
        }
    }

    private static string FormatTime(DateTime createdAt)
    {
        return ToVnLocal(createdAt).ToString("dd/MM/yyyy HH:mm:ss", CultureInfo.InvariantCulture);
    }

    private static DateTime ToVnLocal(DateTime createdAt)
    {
        return TimeZoneInfo.ConvertTimeFromUtc(createdAt, VnTimeZone);
    }

    private static readonly TimeZoneInfo VnTimeZone = ResolveVnTimeZone();

    private static TimeZoneInfo ResolveVnTimeZone()
    {
        // Windows: "SE Asia Standard Time" (UTC+7); Linux/container: "Asia/Ho_Chi_Minh"
        try { return TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time"); }
        catch (TimeZoneNotFoundException) { return TimeZoneInfo.FindSystemTimeZoneById("Asia/Ho_Chi_Minh"); }
        catch (InvalidTimeZoneException) { return TimeZoneInfo.FindSystemTimeZoneById("Asia/Ho_Chi_Minh"); }
    }

    private static string GetPerformerName(AuditLogs row)
    {
        if (!string.IsNullOrWhiteSpace(row.Username))
            return row.Username;
        return row.User?.Username ?? string.Empty;
    }

    private static string GetTargetName(AuditLogs row)
    {
        if (row.TargetUserId.HasValue)
            return row.TargetUser?.Username ?? $"#{row.TargetUserId}";
        if (!string.IsNullOrWhiteSpace(row.EntityName))
            return $"{row.EntityName}{FormatEntityId(row.EntityId)}";
        return string.Empty;
    }

    private static string FormatEntityId(int? entityId)
    {
        return entityId.HasValue ? $" #{entityId}" : string.Empty;
    }

    private static string GetDetailSummary(AuditLogs row)
    {
        var summary = new StringBuilder();
        if (!string.IsNullOrWhiteSpace(row.OldValue))
            summary.Append($"Cũ: {row.OldValue}");
        if (!string.IsNullOrWhiteSpace(row.NewValue))
        {
            if (summary.Length > 0)
                summary.Append(" → ");
            summary.Append($"Mới: {row.NewValue}");
        }

        var text = summary.ToString();
        if (text.Length > MaxDetailLength)
            text = text[..MaxDetailLength] + "...";
        return text;
    }

    private static string EscapeCsv(string value)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;

        if (value.Contains(',') || value.Contains('"') || value.Contains('\n') || value.Contains('\r'))
        {
            return $"\"{value.Replace("\"", "\"\"")}\"";
        }
        return value;
    }

    private async Task LogExportEventAsync(AuditLogsFilterRequest request, int rowCount, string format, CancellationToken cancellationToken)
    {
        // Tự-audit: export dữ liệu audit hàng loạt tự nó là sự kiện nhạy cảm
        await _auditLogService.LogSecurityEventAsync(
            action: "EXPORT",
            eventCategory: "Security", success: true, severity: "Info",
            entityName: "AuditLogs",
            detail: $"Export {format.ToUpperInvariant()} {rowCount:N0} dòng audit log",
            cancellationToken: cancellationToken);
    }
}