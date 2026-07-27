using Domain.Entities.CustomerSatisfaction;
using Hgs.Share.Requests.CustomerSatisfaction;

namespace Core.Interfaces;

public interface ICustomerSatisfactionService
{
    Task<IEnumerable<Devices>> GetDevicesAsync(CancellationToken cancellationToken = default);
    Task<Devices?> GetDeviceByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<Devices> CreateDeviceAsync(DevicesCreateRequest request, CancellationToken cancellationToken = default);
    Task<Devices?> UpdateDeviceAsync(int id, DevicesUpdateRequest request, CancellationToken cancellationToken = default);
    Task<bool> DeleteDeviceAsync(int id, CancellationToken cancellationToken = default);

    Task<IEnumerable<UnsatisfiedReasons>> GetReasonsAsync(CancellationToken cancellationToken = default);
    Task<UnsatisfiedReasons?> GetReasonByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<UnsatisfiedReasons> CreateReasonAsync(UnsatisfiedReasonsCreateRequest request, CancellationToken cancellationToken = default);
    Task<UnsatisfiedReasons?> UpdateReasonAsync(int id, UnsatisfiedReasonsUpdateRequest request, CancellationToken cancellationToken = default);
    Task<bool> DeleteReasonAsync(int id, CancellationToken cancellationToken = default);

    Task<IEnumerable<Evaluations>> GetEvaluationsAsync(CancellationToken cancellationToken = default);
    Task<Evaluations?> GetEvaluationByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<Evaluations> CreateEvaluationAsync(EvaluationsCreateRequest request, CancellationToken cancellationToken = default);
    Task<Evaluations?> UpdateEvaluationAsync(int id, EvaluationsUpdateRequest request, CancellationToken cancellationToken = default);
    Task<bool> DeleteEvaluationAsync(int id, CancellationToken cancellationToken = default);
}
