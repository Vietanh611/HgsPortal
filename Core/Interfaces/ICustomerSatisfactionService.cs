using Domain.Entities.CustomerSatisfaction;
using Hgs.Share.Requests.CustomerSatisfaction;

namespace Core.Interfaces;

public interface ICustomerSatisfactionService
{
    Task<IEnumerable<Devices>> GetDevicesAsync(CancellationToken cancellationToken = default);
    Task<Devices?> GetDeviceByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<Devices> CreateDeviceAsync(DevicesCreateRequest request, CancellationToken cancellationToken = default);
    Task<Devices?> UpdateDeviceAsync(int id, DevicesUpdateRequest request, CancellationToken cancellationToken = default);
    /// <summary>
    /// Xóa thiết bị. Từ chối xóa nếu thiết bị đang được bản đánh giá tham chiếu
    /// — bảo toàn tính toàn vẹn dữ liệu đánh giá đã thu thập.
    /// </summary>
    Task<bool> DeleteDeviceAsync(int id, CancellationToken cancellationToken = default);

    Task<IEnumerable<UnsatisfiedReasons>> GetReasonsAsync(CancellationToken cancellationToken = default);
    Task<UnsatisfiedReasons?> GetReasonByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<UnsatisfiedReasons> CreateReasonAsync(UnsatisfiedReasonsCreateRequest request, CancellationToken cancellationToken = default);
    Task<UnsatisfiedReasons?> UpdateReasonAsync(int id, UnsatisfiedReasonsUpdateRequest request, CancellationToken cancellationToken = default);
    /// <summary>
    /// Xóa lý do chưa hài lòng. Từ chối xóa nếu lý do đang được liên kết với
    /// bản đánh giá — bảo toàn tính toàn vẹn dữ liệu đánh giá đã thu thập.
    /// </summary>
    Task<bool> DeleteReasonAsync(int id, CancellationToken cancellationToken = default);

    Task<IEnumerable<Evaluations>> GetEvaluationsAsync(CancellationToken cancellationToken = default);
    Task<Evaluations?> GetEvaluationByIdAsync(int id, CancellationToken cancellationToken = default);
    /// <summary>
    /// Ghi nhận một đánh giá. Bắt buộc chỉ định ít nhất một trong ba đối tượng
    /// (DeviceId, FlightId hoặc StaffUserId); nếu kèm ReasonIds sẽ gắn các lý do
    /// chưa hài lòng tương ứng vào đánh giá.
    /// </summary>
    Task<Evaluations> CreateEvaluationAsync(EvaluationsCreateRequest request, CancellationToken cancellationToken = default);
    Task<Evaluations?> UpdateEvaluationAsync(int id, EvaluationsUpdateRequest request, CancellationToken cancellationToken = default);
    Task<bool> DeleteEvaluationAsync(int id, CancellationToken cancellationToken = default);
}
