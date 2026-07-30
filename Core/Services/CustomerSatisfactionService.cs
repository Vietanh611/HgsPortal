using Core.Interfaces;
using Data.DbContexts;
using Domain.Entities.CustomerSatisfaction;
using Hgs.Share.Requests.CustomerSatisfaction;
using Microsoft.EntityFrameworkCore;
namespace Core.Services;

public class CustomerSatisfactionService : ICustomerSatisfactionService
{
    private readonly HgsDbContext _dbContext;

    public CustomerSatisfactionService(HgsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IEnumerable<Devices>> GetDevicesAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.Devices
            .AsNoTracking()
            .OrderBy(x => x.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task<Devices?> GetDeviceByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Devices
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<Devices> CreateDeviceAsync(DevicesCreateRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.DeviceName) || string.IsNullOrWhiteSpace(request.DeviceIdentifier))
        {
            throw new ArgumentException("Device name and identifier are required");
        }

        var exists = await _dbContext.Devices
            .AnyAsync(x => x.DeviceIdentifier == request.DeviceIdentifier.Trim(), cancellationToken);

        if (exists)
        {
            throw new InvalidOperationException("Device identifier already exists");
        }

        var device = new Devices
        {
            DeviceName = request.DeviceName.Trim(),
            DeviceIdentifier = request.DeviceIdentifier.Trim(),
            Status = string.IsNullOrWhiteSpace(request.Status) ? "ACTIVE" : request.Status.Trim().ToUpperInvariant(),
            LastSeenAt = request.LastSeenAt
        };

        _dbContext.Devices.Add(device);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return device;
    }

    public async Task<Devices?> UpdateDeviceAsync(int id, DevicesUpdateRequest request, CancellationToken cancellationToken = default)
    {
        var device = await _dbContext.Devices.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (device is null)
        {
            return null;
        }

        if (!string.IsNullOrWhiteSpace(request.DeviceName))
        {
            device.DeviceName = request.DeviceName.Trim();
        }

        if (!string.IsNullOrWhiteSpace(request.DeviceIdentifier))
        {
            var identifierExists = await _dbContext.Devices
                .AnyAsync(x => x.DeviceIdentifier == request.DeviceIdentifier.Trim() && x.Id != id, cancellationToken);

            if (identifierExists)
            {
                throw new InvalidOperationException("Device identifier already exists");
            }

            device.DeviceIdentifier = request.DeviceIdentifier.Trim();
        }

        if (!string.IsNullOrWhiteSpace(request.Status))
        {
            device.Status = request.Status.Trim().ToUpperInvariant();
        }

        if (request.LastSeenAt.HasValue)
        {
            device.LastSeenAt = request.LastSeenAt;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        return device;
    }

    public async Task<bool> DeleteDeviceAsync(int id, CancellationToken cancellationToken = default)
    {
        var device = await _dbContext.Devices.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (device is null)
        {
            return false;
        }

        var hasEvaluations = await _dbContext.Evaluations.AnyAsync(x => x.DeviceId == id, cancellationToken);
        if (hasEvaluations)
        {
            throw new InvalidOperationException("Cannot delete device because it is referenced by evaluations");
        }

        _dbContext.Devices.Remove(device);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<IEnumerable<UnsatisfiedReasons>> GetReasonsAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.UnsatisfiedReasons
            .AsNoTracking()
            .OrderBy(x => x.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task<UnsatisfiedReasons?> GetReasonByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.UnsatisfiedReasons
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<UnsatisfiedReasons> CreateReasonAsync(UnsatisfiedReasonsCreateRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.ReasonName))
        {
            throw new ArgumentException("Reason name is required");
        }

        var exists = await _dbContext.UnsatisfiedReasons
            .AnyAsync(x => x.ReasonName == request.ReasonName.Trim(), cancellationToken);

        if (exists)
        {
            throw new InvalidOperationException("Reason name already exists");
        }

        var reason = new UnsatisfiedReasons
        {
            ReasonName = request.ReasonName.Trim(),
            Status = string.IsNullOrWhiteSpace(request.Status) ? "ACTIVE" : request.Status.Trim().ToUpperInvariant()
        };

        _dbContext.UnsatisfiedReasons.Add(reason);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return reason;
    }

    public async Task<UnsatisfiedReasons?> UpdateReasonAsync(int id, UnsatisfiedReasonsUpdateRequest request, CancellationToken cancellationToken = default)
    {
        var reason = await _dbContext.UnsatisfiedReasons.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (reason is null)
        {
            return null;
        }

        if (!string.IsNullOrWhiteSpace(request.ReasonName))
        {
            var nameExists = await _dbContext.UnsatisfiedReasons
                .AnyAsync(x => x.ReasonName == request.ReasonName.Trim() && x.Id != id, cancellationToken);

            if (nameExists)
            {
                throw new InvalidOperationException("Reason name already exists");
            }

            reason.ReasonName = request.ReasonName.Trim();
        }

        if (!string.IsNullOrWhiteSpace(request.Status))
        {
            reason.Status = request.Status.Trim().ToUpperInvariant();
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        return reason;
    }

    public async Task<bool> DeleteReasonAsync(int id, CancellationToken cancellationToken = default)
    {
        var reason = await _dbContext.UnsatisfiedReasons.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (reason is null)
        {
            return false;
        }

        var hasLinks = await _dbContext.EvaluationReasonLinks.AnyAsync(x => x.ReasonId == id, cancellationToken);
        if (hasLinks)
        {
            throw new InvalidOperationException("Cannot delete reason because it is referenced by evaluations");
        }

        _dbContext.UnsatisfiedReasons.Remove(reason);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<IEnumerable<Evaluations>> GetEvaluationsAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.Evaluations
            .Include(x => x.Device)
            .Include(x => x.EvaluationReasonLinks)
                .ThenInclude(x => x.Reason)
            .AsNoTracking()
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<Evaluations?> GetEvaluationByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Evaluations
            .Include(x => x.Device)
            .Include(x => x.EvaluationReasonLinks)
                .ThenInclude(x => x.Reason)
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<Evaluations> CreateEvaluationAsync(EvaluationsCreateRequest request, CancellationToken cancellationToken = default)
    {
        if (request.DeviceId is null && request.FlightId is null && request.StaffUserId is null)
        {
            throw new ArgumentException("At least one of DeviceId, FlightId, or StaffUserId is required");
        }

        if (request.DeviceId.HasValue)
        {
            var deviceExists = await _dbContext.Devices.AnyAsync(x => x.Id == request.DeviceId.Value, cancellationToken);
            if (!deviceExists)
            {
                throw new KeyNotFoundException("Device not found");
            }
        }

        if (request.ReasonIds is not null && request.ReasonIds.Count > 0)
        {
            var reasonIds = request.ReasonIds.Distinct().ToList();
            var existingReasonCount = await _dbContext.UnsatisfiedReasons
                .Where(x => reasonIds.Contains(x.Id))
                .CountAsync(cancellationToken);

            if (existingReasonCount != reasonIds.Count)
            {
                throw new KeyNotFoundException("One or more reason ids are invalid");
            }
        }

        var evaluation = new Evaluations
        {
            FlightId = request.FlightId,
            StaffUserId = request.StaffUserId,
            DeviceId = request.DeviceId,
            CheckinCounterName = request.CheckinCounterName,
            RatingLevel = request.RatingLevel,
            EvaluationType = request.EvaluationType
        };

        _dbContext.Evaluations.Add(evaluation);
        await _dbContext.SaveChangesAsync(cancellationToken);

        if (request.ReasonIds is not null && request.ReasonIds.Count > 0)
        {
            var reasonIds = request.ReasonIds.Distinct().ToList();
            foreach (var reasonId in reasonIds)
            {
                _dbContext.EvaluationReasonLinks.Add(new EvaluationReasonLinks
                {
                    EvaluationId = evaluation.Id,
                    ReasonId = reasonId
                });
            }

            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        return evaluation;
    }

    public async Task<Evaluations?> UpdateEvaluationAsync(int id, EvaluationsUpdateRequest request, CancellationToken cancellationToken = default)
    {
        var evaluation = await _dbContext.Evaluations
            .Include(x => x.EvaluationReasonLinks)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (evaluation is null)
        {
            return null;
        }

        if (request.FlightId.HasValue)
        {
            evaluation.FlightId = request.FlightId.Value;
        }

        if (request.StaffUserId.HasValue)
        {
            evaluation.StaffUserId = request.StaffUserId.Value;
        }

        if (request.DeviceId.HasValue)
        {
            var deviceExists = await _dbContext.Devices.AnyAsync(x => x.Id == request.DeviceId.Value, cancellationToken);
            if (!deviceExists)
            {
                throw new KeyNotFoundException("Device not found");
            }

            evaluation.DeviceId = request.DeviceId.Value;
        }

        if (request.CheckinCounterName is not null)
        {
            evaluation.CheckinCounterName = request.CheckinCounterName;
        }

        if (request.RatingLevel.HasValue)
        {
            evaluation.RatingLevel = request.RatingLevel.Value;
        }

        if (request.EvaluationType.HasValue)
        {
            evaluation.EvaluationType = request.EvaluationType.Value;
        }

        if (request.ReasonIds is not null)
        {
            var reasonIds = request.ReasonIds.Distinct().ToList();
            var existingReasonCount = await _dbContext.UnsatisfiedReasons
                .Where(x => reasonIds.Contains(x.Id))
                .CountAsync(cancellationToken);

            if (existingReasonCount != reasonIds.Count)
            {
                throw new KeyNotFoundException("One or more reason ids are invalid");
            }

            var currentLinks = evaluation.EvaluationReasonLinks.ToList();
            _dbContext.EvaluationReasonLinks.RemoveRange(currentLinks);

            foreach (var reasonId in reasonIds)
            {
                _dbContext.EvaluationReasonLinks.Add(new EvaluationReasonLinks
                {
                    EvaluationId = evaluation.Id,
                    ReasonId = reasonId
                });
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        return evaluation;
    }

    public async Task<bool> DeleteEvaluationAsync(int id, CancellationToken cancellationToken = default)
    {
        var evaluation = await _dbContext.Evaluations
            .Include(x => x.EvaluationReasonLinks)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (evaluation is null)
        {
            return false;
        }

        if (evaluation.EvaluationReasonLinks.Count > 0)
        {
            _dbContext.EvaluationReasonLinks.RemoveRange(evaluation.EvaluationReasonLinks);
        }

        _dbContext.Evaluations.Remove(evaluation);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }
}
