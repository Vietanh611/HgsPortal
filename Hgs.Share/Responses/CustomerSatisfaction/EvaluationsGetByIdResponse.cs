namespace Hgs.Share.Responses.CustomerSatisfaction;

public class EvaluationsGetByIdResponse
{
    public int Id { get; set; }
    public int FlightId { get; set; }
    public int DeviceId { get; set; }
    public string? DeviceName { get; set; }
    public int Rating { get; set; }
    public string? Comment { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<int>? ReasonIds { get; set; }
}
