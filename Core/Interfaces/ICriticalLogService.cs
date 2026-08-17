using Hgs.Share.Requests.CriticalLogs;
using Hgs.Share.Responses.ApiResponses;
using Hgs.Share.Responses.CriticalLogs;

namespace Core.Interfaces;

/// <summary>
/// Đọc nhật ký lỗi hệ thống từ bảng <c>CriticalLogs</c> (do Serilog ghi).
/// Chỉ đọc — bảng không thuộc vòng đời EF Core (script SQL quản lý).
/// </summary>
public interface ICriticalLogService
{
    /// <summary>
    /// Lọc log theo level/khoảng thời gian/từ khóa, phân trang.
    /// Clamp PageNumber ≥ 1, PageSize ∈ [1, 200] — chống DoS qua [FromQuery].
    /// </summary>
    Task<PagedResponse<CriticalLogsGetAllResponse>> GetFilteredAsync(
        CriticalLogsFilterRequest request,
        CancellationToken cancellationToken = default);

    Task<CriticalLogsGetAllResponse?> GetByIdAsync(
        long id,
        CancellationToken cancellationToken = default);
}
