using Domain.Entities.DisplayDevices;
using Hgs.Share.Requests.DisplayDevices;

namespace Core.Interfaces;

public interface IDisplayDevicesService
{
    Task<IEnumerable<DisplayDevices>> GetDisplayDevicesAsync(CancellationToken cancellationToken = default);
    Task<DisplayDevices?> GetDisplayDeviceByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<DisplayDevices> CreateDeviceAsync(DisplayDevicesCreateRequest request, CancellationToken cancellationToken = default);
}