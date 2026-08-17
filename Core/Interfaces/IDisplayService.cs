using Hgs.Share.Dtos;

namespace Core.Interfaces
{
    /// <summary>
    /// Cung cấp dữ liệu hiển thị hành lý đến (baggage arrival) cho màn hình
    /// đại sảnh. Dữ liệu lấy từ stored procedure trong DB ACDM legacy.
    /// </summary>
    public interface IDisplayService
    {
        /// <summary>Danh sách hành lý đến luồng quốc tế dùng cho màn hình hiển thị.</summary>
        Task<List<BaggageArrivalDisplayDto>> GetInternationalBaggageArrivalDisplayAsync(CancellationToken cancellationToken = default);
        /// <summary>Danh sách hành lý đến luồng nội địa dùng cho màn hình hiển thị.</summary>
        Task<List<BaggageArrivalDisplayDto>> GetDomesticBaggageArrivalDisplayAsync(CancellationToken cancellationToken = default);
    }
}
