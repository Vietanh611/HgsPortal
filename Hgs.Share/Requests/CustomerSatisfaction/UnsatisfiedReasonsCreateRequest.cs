namespace Hgs.Share.Requests.CustomerSatisfaction;

public class UnsatisfiedReasonsCreateRequest
{
    public string ReasonName { get; set; } = string.Empty;
    public string? Status { get; set; }
}
