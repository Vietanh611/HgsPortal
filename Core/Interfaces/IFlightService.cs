using Domain.Entities.ACDM;

namespace Core.Interfaces;

public interface IFlightService
{
    Task<FlightACDM?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<IEnumerable<FlightACDM>> GetByFlightNoAndDateAsync(string? flightNo, string? flightDate, CancellationToken cancellationToken = default);
}
