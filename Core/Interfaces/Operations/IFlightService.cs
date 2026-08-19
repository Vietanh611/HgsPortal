using Domain.Entities.ACDM;

namespace Core.Interfaces.Operations;

/// <summary>
/// Đọc dữ liệu chuyến bay từ cơ sở dữ liệu ACDM (AcdmContext — DB legacy riêng,
/// tách khỏi HgsDbContext của portal). Chỉ đọc, không ghi.
/// </summary>
public interface IFlightService
{
    Task<FlightACDM?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    /// <summary>
    /// Tìm chuyến bay theo số hiệu và/hoặc ngày bay, sắp xếp theo thời gian bay.
    /// Cả hai điều kiện đều tùy chọn; <paramref name="flightDate"/> được so khớp
    /// dạng chuỗi vì bảng legacy lưu ngày dưới dạng text (không phải DateTime).
    /// </summary>
    Task<IEnumerable<FlightACDM>> GetByFlightNoAndDateAsync(string? flightNo, string? flightDate, CancellationToken cancellationToken = default);
}
