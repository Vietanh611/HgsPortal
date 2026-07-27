namespace Hgs.Share.Requests.CustomerSatisfaction;

public class EvaluationsUpdateRequest
{
    public int? FlightId { get; set; }
    public int? DeviceId { get; set; }
    public int? Rating { get; set; }
    public string? Comment { get; set; }
    public List<int>? ReasonIds { get; set; }
}
