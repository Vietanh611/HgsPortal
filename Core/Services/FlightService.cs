using Core.Interfaces;
using Data.DbContexts;
using Domain.Entities.ACDM;
using Microsoft.EntityFrameworkCore;

namespace Core.Services;

public class FlightService : IFlightService
{
    private readonly AcdmContext _dbContext;

    public FlightService(AcdmContext dbContext)
    {
        _dbContext = dbContext;
    }


    public async Task<FlightACDM?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Flight
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<IEnumerable<FlightACDM>> GetByFlightNoAndDateAsync(string? flightNo, string? flightDate, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Flight.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(flightNo))
        {
            query = query.Where(x => x.FlightNo == flightNo);
        }

        if (!string.IsNullOrWhiteSpace(flightDate))
        {
            query = query.Where(x => x.FlightDate == flightDate);
        }

        return await query
            .OrderBy(x => x.FlightDateTime)
            .ToListAsync(cancellationToken);
    }

}
