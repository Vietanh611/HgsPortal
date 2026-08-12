namespace Hgs.Share.Dtos
{
    public class BaggageArrivalDisplayDto
    {
        public int FlightId { get; set; }
        public string FlightNo { get; set; } = string.Empty;
        public string Path { get; set; } = string.Empty;
        public string Route { get; set; } = string.Empty;
        public string NameCity { get; set; } = string.Empty;
        public string? Belt { get; set; }
    }
}
