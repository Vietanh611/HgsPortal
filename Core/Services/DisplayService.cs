using Core.Interfaces;
using Data.DbContexts;
using Hgs.Share.Dtos;
using Microsoft.EntityFrameworkCore;

namespace Core.Services
{
    public class DisplayService : IDisplayService
    {
        private readonly AcdmContext _dbContext;
        public DisplayService(AcdmContext dbContext)
        {
            _dbContext = dbContext;
        }
        private const int Domestic = 0;
        private const int International = 1;
        public async Task<List<BaggageArrivalDisplayDto>> GetDomesticBaggageArrivalDisplayAsync(CancellationToken cancellationToken = default)
        {
            return await _dbContext.Database.SqlQuery<BaggageArrivalDisplayDto>($"EXEC SP_GetListProcessingArrival {Domestic}").ToListAsync(cancellationToken);
        }

        public async Task<List<BaggageArrivalDisplayDto>> GetInternationalBaggageArrivalDisplayAsync(CancellationToken cancellationToken = default)
        {
            return await _dbContext.Database.SqlQuery<BaggageArrivalDisplayDto>($"EXEC SP_GetListProcessingArrival {International}").ToListAsync(cancellationToken);
        }
    }
}
