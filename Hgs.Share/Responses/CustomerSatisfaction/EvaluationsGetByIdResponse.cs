namespace Hgs.Share.Responses.CustomerSatisfaction;

public class EvaluationsGetByIdResponse
{
    public int Id { get; set; }
    public int? FlightId { get; set; }
    public int? StaffUserId { get; set; }
    public int? DeviceId { get; set; }
    public string? DeviceName { get; set; }
    public string? CheckinCounterName { get; set; }
    public int RatingLevel { get; set; }
    public string? EvaluationType { get; set; }
    public List<int>? ReasonIds { get; set; }
}
