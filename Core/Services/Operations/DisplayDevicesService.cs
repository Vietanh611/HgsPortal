using Core.Interfaces;
using Data.DbContexts;
using Domain.Entities.DisplayDevices;
using Hgs.Share.Requests.DisplayDevices;
using Microsoft.EntityFrameworkCore;

namespace Core.Services.Operations;

public class DisplayDevicesService : IDisplayDevicesService
{
    private readonly HgsDbContext _dbContext;

    public DisplayDevicesService(HgsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IEnumerable<DisplayDevices>> GetDisplayDevicesAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.DisplayDevices
            .AsNoTracking()
            .OrderBy(x => x.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task<DisplayDevices?> GetDisplayDeviceByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.DisplayDevices
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<DisplayDevices> CreateDeviceAsync(DisplayDevicesCreateRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.DeviceName) || string.IsNullOrWhiteSpace(request.DeviceIdentifier))
        {
            throw new ArgumentException("Device name and identifier are required");
        }

        var exists = await _dbContext.DisplayDevices
            .AnyAsync(x => x.DeviceIdentifier == request.DeviceIdentifier.Trim(), cancellationToken);

        if (exists)
        {
            throw new InvalidOperationException("Device identifier already exists");
        }

        var device = new DisplayDevices
        {
            DeviceName = request.DeviceName.Trim(),
            DeviceIdentifier = request.DeviceIdentifier.Trim(),
            Status = string.IsNullOrWhiteSpace(request.Status) ? "ACTIVE" : request.Status.Trim().ToUpperInvariant(),
            LastSeenAt = request.LastSeenAt,
            IsEnabled = request.IsEnabled
        };

        _dbContext.DisplayDevices.Add(device);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return device;
    }
}