namespace Hgs.Share.Responses.CriticalLogs;

public class CriticalLogsGetAllResponse
{
    public long Id { get; set; }
    public string? Message { get; set; }
    public string? MessageTemplate { get; set; }
    public string? Level { get; set; }
    public DateTime TimeStamp { get; set; }
    public string? Exception { get; set; }
    public string? Properties { get; set; }
    public string? RequestId { get; set; }
    public string? User { get; set; }
    public string? Path { get; set; }
    public string? Method { get; set; }
}
