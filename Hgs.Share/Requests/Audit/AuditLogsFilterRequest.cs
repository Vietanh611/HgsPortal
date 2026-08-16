namespace Hgs.Share.Requests.Audit;

public class AuditLogsFilterRequest
{
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;

    public string? EntityName { get; set; }
    public int? EntityId { get; set; }
    public int? UserId { get; set; }
    public int? TargetUserId { get; set; }
    public string? EventCategory { get; set; }
    public string? Action { get; set; }
    public bool? Success { get; set; }
    public string? Severity { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public string? Keyword { get; set; }
}