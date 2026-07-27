namespace Hgs.Share.Responses.CustomerSatisfaction;

public class UnsatisfiedReasonsGetByIdResponse
{
    public int Id { get; set; }
    public string ReasonName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
}
