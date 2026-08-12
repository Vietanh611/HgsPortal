using Hgs.Share.Dtos;

namespace Core.Interfaces
{
    public interface IDisplayService
    {
        Task<List<BaggageArrivalDisplayDto>> GetInternationalBaggageArrivalDisplayAsync(CancellationToken cancellationToken = default);
        Task<List<BaggageArrivalDisplayDto>> GetDomesticBaggageArrivalDisplayAsync(CancellationToken cancellationToken = default);
    }
}
