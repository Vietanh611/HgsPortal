using Core.Interfaces.Auth;
using Core.Interfaces.Operations;
using Core.Services.Settings;
using Data.DbContexts;
using Domain.Entities.DeviceManagement;
using Hgs.Share.Exceptions;
using Hgs.Share.Requests.Devices;
using Hgs.Share.Responses.Devices;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Security.Cryptography;

namespace Core.Services.Operations;

public class DevicesService : IDevicesService
{
    private readonly HgsDbContext _dbContext;
    private readonly ITokenService _tokenService;
    private readonly IAuditLogService _auditLog;
    private readonly DeviceAuthSettings _deviceAuthSettings;

    public DevicesService(
        HgsDbContext dbContext,
        ITokenService tokenService,
        IAuditLogService auditLog,
        IOptions<DeviceAuthSettings> deviceAuthSettings)
    {
        _dbContext = dbContext;
        _tokenService = tokenService;
        _auditLog = auditLog;
        _deviceAuthSettings = deviceAuthSettings.Value;
    }

    public async Task<DevicePairingCodeCreateResponse> CreatePairingCodeAsync(DevicePairingCodeCreateRequest request, int? createdBy, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.DeviceName))
        {
            throw new ArgumentException("Device name is required");
        }

        var deviceType = string.IsNullOrWhiteSpace(request.DeviceType) ? DeviceTypes.KioskWeb : request.DeviceType.Trim();
        if (!DeviceTypes.IsValid(deviceType))
        {
            throw new ArgumentException($"Device type '{deviceType}' is not supported");
        }

        var deviceIdentifier = Guid.NewGuid().ToString("N").ToUpperInvariant();
        var pairingCode = _tokenService.GeneratePairingCode();

        var now = DateTime.UtcNow;
        var device = new Device
        {
            DeviceType = deviceType,
            DeviceName = request.DeviceName.Trim(),
            DeviceIdentifier = deviceIdentifier,
            Status = DeviceStatuses.Pending,
            PairingCodeHash = _tokenService.HashPairingCode(pairingCode),
            PairingCodeExpiresAt = now.AddMinutes(_deviceAuthSettings.PairingCodeTtlMinutes),
            OrganizationUnitId = request.OrganizationUnitId,
            IsEnabled = true,
            IsDeleted = false,
            CreatedAt = now,
            CreatedBy = createdBy
        };

        _dbContext.ManagedDevices.Add(device);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _auditLog.Log(
            "CREATE_DEVICE_PAIRING_CODE",
            "Devices",
            device.Id,
            null,
            new { device.DeviceName, device.DeviceType, device.DeviceIdentifier, device.OrganizationUnitId, PairingCodeExpiresAt = device.PairingCodeExpiresAt });

        await _dbContext.SaveChangesAsync(cancellationToken);

        return new DevicePairingCodeCreateResponse
        {
            DeviceRowId = device.Id,
            DeviceIdentifier = device.DeviceIdentifier,
            DeviceName = device.DeviceName,
            DeviceType = device.DeviceType,
            OrganizationUnitId = device.OrganizationUnitId,
            PairingCode = pairingCode,
            ExpiresAt = device.PairingCodeExpiresAt!.Value
        };
    }

    public async Task<IEnumerable<DeviceGetAllResponse>> GetAllAsync(
        string? status,
        int? organizationUnitId,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.ManagedDevices
            .Include(d => d.OrganizationUnit)
            .AsNoTracking()
            .Where(d => !d.IsDeleted);

        if (organizationUnitId.HasValue)
        {
            query = query.Where(d => d.OrganizationUnitId == organizationUnitId.Value);
        }

        var devices = await query
            .OrderByDescending(d => d.LastSeenAt)
            .ThenByDescending(d => d.CreatedAt)
            .ToListAsync(cancellationToken);

        var result = devices.Select(MapToGetAllResponse).ToList();

        if (!string.IsNullOrWhiteSpace(status))
        {
            result = result.Where(r => string.Equals(r.Status, status, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        return result;
    }

    public async Task<DeviceGetByIdResponse?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var device = await _dbContext.ManagedDevices
            .Include(d => d.RevokedByUser)
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.Id == id && !d.IsDeleted, cancellationToken);

        if (device is null)
        {
            return null;
        }

        return MapToGetByIdResponse(device);
    }

    public async Task<DeviceStatusUpdateResponse> UpdateStatusAsync(int id, bool isEnabled, int? updatedBy, CancellationToken cancellationToken = default)
    {
        var device = await GetTrackedDeviceAsync(id, cancellationToken);
        if (device is null || device.IsDeleted)
        {
            throw new NotFoundException("Thiết bị không tồn tại");
        }

        if (device.Status == DeviceStatuses.Revoked || device.RevokedAt.HasValue)
        {
            throw new BusinessRuleException("Không thể đổi trạng thái thiết bị đã bị thu hồi");
        }

        device.IsEnabled = isEnabled;

        _auditLog.Log(
            "UPDATE_DEVICE_STATUS",
            "Devices",
            device.Id,
            new { IsEnabled = !isEnabled },
            new { IsEnabled = isEnabled });

        await _dbContext.SaveChangesAsync(cancellationToken);

        return new DeviceStatusUpdateResponse
        {
            Id = device.Id,
            DeviceType = device.DeviceType,
            DeviceName = device.DeviceName,
            DeviceIdentifier = device.DeviceIdentifier,
            IsEnabled = device.IsEnabled,
            Status = ResolveStatus(device)
        };
    }

    public async Task<DeviceRevokeResponse> RevokeAsync(int id, int? revokedBy, CancellationToken cancellationToken = default)
    {
        var device = await GetTrackedDeviceAsync(id, cancellationToken);
        if (device is null || device.IsDeleted)
        {
            throw new NotFoundException("Thiết bị không tồn tại");
        }

        if (device.Status == DeviceStatuses.Revoked || device.RevokedAt.HasValue)
        {
            throw new BusinessRuleException("Thiết bị đã bị thu hồi trước đó");
        }

        var now = DateTime.UtcNow;
        device.Status = DeviceStatuses.Revoked;
        device.RevokedAt = now;
        device.RevokedBy = revokedBy;
        device.IsEnabled = false;

        _auditLog.Log(
            "REVOKE_DEVICE",
            "Devices",
            device.Id,
            null,
            new { device.DeviceName, device.DeviceIdentifier, RevokedAt = now });

        await _dbContext.SaveChangesAsync(cancellationToken);

        return new DeviceRevokeResponse
        {
            Id = device.Id,
            DeviceType = device.DeviceType,
            DeviceName = device.DeviceName,
            DeviceIdentifier = device.DeviceIdentifier,
            Status = DeviceStatuses.Revoked,
            RevokedAt = device.RevokedAt
        };
    }

    public async Task<DevicePairingCodeRegenerateResponse> RegeneratePairingCodeAsync(int id, int? requestedBy, CancellationToken cancellationToken = default)
    {
        var device = await GetTrackedDeviceAsync(id, cancellationToken);
        if (device is null || device.IsDeleted)
        {
            throw new NotFoundException("Thiết bị không tồn tại");
        }

        if (device.Status == DeviceStatuses.Active)
        {
            throw new BusinessRuleException("Thiết bị đang hoạt động — không thể tạo mã ghép mới");
        }

        var pairingCode = _tokenService.GeneratePairingCode();
        var now = DateTime.UtcNow;

        // Reset về PENDING, xoá key cũ (thiết bị phải pairing lại).
        device.Status = DeviceStatuses.Pending;
        device.DeviceKeyHash = null;
        device.PairingCodeHash = _tokenService.HashPairingCode(pairingCode);
        device.PairingCodeExpiresAt = now.AddMinutes(_deviceAuthSettings.PairingCodeTtlMinutes);

        _auditLog.Log(
            "REGENERATE_DEVICE_PAIRING_CODE",
            "Devices",
            device.Id,
            new { device.DeviceName, device.DeviceIdentifier },
            new { device.DeviceName, device.DeviceIdentifier, PairingCodeExpiresAt = device.PairingCodeExpiresAt });

        await _dbContext.SaveChangesAsync(cancellationToken);

        return new DevicePairingCodeRegenerateResponse
        {
            DeviceRowId = device.Id,
            DeviceIdentifier = device.DeviceIdentifier,
            DeviceName = device.DeviceName,
            PairingCode = pairingCode,
            ExpiresAt = device.PairingCodeExpiresAt!.Value
        };
    }

    public async Task DeleteAsync(int id, int? deletedBy, CancellationToken cancellationToken = default)
    {
        var device = await GetTrackedDeviceAsync(id, cancellationToken);
        if (device is null || device.IsDeleted)
        {
            throw new NotFoundException("Thiết bị không tồn tại");
        }

        if (!device.RevokedAt.HasValue)
        {
            throw new BusinessRuleException("Chỉ được xoá thiết bị đã bị thu hồi");
        }

        device.IsDeleted = true;
        device.DeletedAt = DateTime.UtcNow;
        device.DeletedBy = deletedBy;

        _auditLog.Log(
            "DELETE_DEVICE",
            "Devices",
            device.Id,
            new { device.DeviceName, device.DeviceIdentifier },
            null);

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<DevicePairResponse> PairDeviceAsync(string pairingCode, string? ipAddress, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(pairingCode))
        {
            throw new BadRequestException("Mã không hợp lệ hoặc đã hết hạn");
        }

        var pairingCodeHash = _tokenService.HashPairingCode(pairingCode.Trim().ToUpperInvariant());
        var now = DateTime.UtcNow;

        var device = await _dbContext.ManagedDevices
            .FirstOrDefaultAsync(d =>
                d.Status == DeviceStatuses.Pending &&
                d.PairingCodeHash == pairingCodeHash &&
                !d.IsDeleted, cancellationToken);

        if (device is null || !device.PairingCodeExpiresAt.HasValue || device.PairingCodeExpiresAt.Value < now)
        {
            await _auditLog.LogSecurityEventAsync(
                action: "DEVICE_PAIR_FAILED",
                eventCategory: "Auth",
                success: false,
                severity: "Warning",
                username: null,
                entityName: "Devices",
                entityId: device?.Id,
                detail: "Invalid or expired pairing code attempt",
                cancellationToken: cancellationToken);

            throw new BadRequestException("Mã không hợp lệ hoặc đã hết hạn");
        }

        var deviceKey = _tokenService.GenerateDeviceKey();

        device.DeviceKeyHash = _tokenService.HashDeviceKey(deviceKey);
        device.Status = DeviceStatuses.Active;
        device.IsEnabled = true;
        device.PairingCodeHash = null;
        device.PairingCodeExpiresAt = null;
        device.LastSeenAt = now;
        device.LastSeenIp = ipAddress;

        _auditLog.Log(
            "DEVICE_PAIRED",
            "Devices",
            device.Id,
            new { device.DeviceName, device.DeviceIdentifier },
            new { device.DeviceName, device.DeviceIdentifier, Status = DeviceStatuses.Active });

        await _dbContext.SaveChangesAsync(cancellationToken);

        return new DevicePairResponse
        {
            DeviceId = device.DeviceIdentifier,
            DeviceType = device.DeviceType,
            DeviceName = device.DeviceName,
            DeviceKey = deviceKey
        };
    }

    public async Task<Device?> AuthenticateDeviceAsync(string deviceIdentifier, string deviceKey, string? expectedDeviceType = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(deviceIdentifier) || string.IsNullOrWhiteSpace(deviceKey))
        {
            return null;
        }

        var device = await _dbContext.ManagedDevices
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.DeviceIdentifier == deviceIdentifier.Trim(), cancellationToken);

        if (device is null || device.IsDeleted || !device.IsEnabled || device.Status != DeviceStatuses.Active)
        {
            return null;
        }

        if (!string.IsNullOrWhiteSpace(expectedDeviceType) && device.DeviceType != expectedDeviceType)
        {
            return null;
        }

        if (string.IsNullOrEmpty(device.DeviceKeyHash))
        {
            return null;
        }

        var keyHash = _tokenService.HashDeviceKey(deviceKey);
        if (!HashesEqual(device.DeviceKeyHash, keyHash))
        {
            return null;
        }

        return device;
    }

    public async Task UpdateLastSeenAtAsync(int deviceId, string? ipAddress, CancellationToken cancellationToken = default)
    {
        await _dbContext.ManagedDevices
            .Where(d => d.Id == deviceId)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(d => d.LastSeenAt, DateTime.UtcNow)
                .SetProperty(d => d.LastSeenIp, ipAddress),
            cancellationToken);
    }

    private async Task<Device?> GetTrackedDeviceAsync(int id, CancellationToken cancellationToken)
    {
        return await _dbContext.ManagedDevices
            .FirstOrDefaultAsync(d => d.Id == id && !d.IsDeleted, cancellationToken);
    }

    private string ResolveStatus(Device device)
    {
        if (device.Status == DeviceStatuses.Revoked || device.RevokedAt.HasValue)
        {
            return "REVOKED";
        }

        if (!device.IsEnabled)
        {
            return "DISABLED";
        }

        if (device.Status == DeviceStatuses.Pending)
        {
            return "PENDING";
        }

        if (device.LastSeenAt.HasValue &&
            device.LastSeenAt.Value >= DateTime.UtcNow.AddMinutes(-_deviceAuthSettings.OnlineThresholdMinutes))
        {
            return "ONLINE";
        }

        return "ACTIVE";
    }

    private static bool HashesEqual(string stored, string computed)
    {
        try
        {
            var storedBytes = Convert.FromBase64String(stored);
            var computedBytes = Convert.FromBase64String(computed);

            if (storedBytes.Length != computedBytes.Length)
            {
                return false;
            }

            return CryptographicOperations.FixedTimeEquals(storedBytes, computedBytes);
        }
        catch (FormatException)
        {
            return false;
        }
    }

    private DeviceGetAllResponse MapToGetAllResponse(Device device) => new()
    {
        Id = device.Id,
        DeviceType = device.DeviceType,
        DeviceName = device.DeviceName,
        DeviceIdentifier = device.DeviceIdentifier,
        OrganizationUnitId = device.OrganizationUnitId,
        Status = ResolveStatus(device),
        LastSeenAt = device.LastSeenAt,
        IsEnabled = device.IsEnabled
    };

    private DeviceGetByIdResponse MapToGetByIdResponse(Device device) => new()
    {
        Id = device.Id,
        DeviceType = device.DeviceType,
        DeviceName = device.DeviceName,
        DeviceIdentifier = device.DeviceIdentifier,
        OrganizationUnitId = device.OrganizationUnitId,
        IsEnabled = device.IsEnabled,
        Status = ResolveStatus(device),
        RevokedAt = device.RevokedAt,
        RevokedByUserName = device.RevokedByUser?.Username,
        LastSeenAt = device.LastSeenAt,
        CreatedAt = device.CreatedAt,
        CreatedBy = device.CreatedBy,
        IsDeleted = device.IsDeleted,
        DeletedAt = device.DeletedAt
    };
}
