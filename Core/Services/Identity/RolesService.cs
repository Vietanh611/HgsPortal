using Core.Constants;
using Core.Interfaces.Identity;
using Core.Interfaces.Notifications;
using Core.Interfaces.Operations;
using Data.DbContexts;
using Domain.Entities.Identity;
using Hgs.Share.Requests.Notifications;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Core.Services.Identity;

public class RolesService : IRolesService
{
    private readonly HgsDbContext _dbContext;
    private readonly IAuditLogService _auditLog;
    private readonly INotificationService _notificationService;
    private readonly ILogger<RolesService> _logger;

    public RolesService(HgsDbContext dbContext, IAuditLogService auditLog, INotificationService notificationService, ILogger<RolesService> logger)
    {
        _dbContext = dbContext;
        _auditLog = auditLog;
        _notificationService = notificationService;
        _logger = logger;
    }

    public async Task<IEnumerable<Roles>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.Roles
            .Include(x => x.OrganizationUnit)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<Roles?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Roles
            .Include(x => x.OrganizationUnit)
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<Roles> CreateAsync(Roles request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Code) || string.IsNullOrWhiteSpace(request.Name))
        {
            throw new ArgumentException("Code and name are required");
        }

        var exists = await _dbContext.Roles
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Code == request.Code.Trim(), cancellationToken);
        if (exists is not null)
        {
            throw new InvalidOperationException("Role code already exists");
        }

        _dbContext.Roles.Add(request);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _auditLog.Log(
            action: "CREATE",
            entityName: "Roles",
            entityId: request.Id,
            oldValue: null,
            newValue: request);

        await _dbContext.SaveChangesAsync(cancellationToken);
        return request;
    }

    public async Task<Roles?> UpdateAsync(int id, Roles request, CancellationToken cancellationToken = default)
    {
        var role = await _dbContext.Roles
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (role is null)
        {
            return null;
        }

        var oldSnapshot = new
        {
            role.Id,
            role.Code,
            role.Name,
            role.Description,
            role.OrganizationUnitId,
            role.DataScope,
            role.IsSystemRole,
            role.IsActive
        };

        if (!string.IsNullOrWhiteSpace(request.Code))
        {
            var exists = await _dbContext.Roles
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Code == request.Code.Trim(), cancellationToken);
            if (exists is not null && exists.Id != id)
            {
                throw new InvalidOperationException("Role code already exists");
            }

            role.Code = request.Code.Trim();
        }

        if (!string.IsNullOrWhiteSpace(request.Name))
        {
            role.Name = request.Name.Trim();
        }

        if (request.Description is not null)
        {
            role.Description = request.Description;
        }

        if (request.OrganizationUnitId.HasValue)
        {
            role.OrganizationUnitId = request.OrganizationUnitId;
        }

        if (request.DataScope is not null)
        {
            role.DataScope = request.DataScope;
        }

        if (request.IsSystemRole)
        {
            role.IsSystemRole = request.IsSystemRole;
        }

        if (request.IsActive)
        {
            role.IsActive = request.IsActive;
        }

        // Role hệ thống bị sửa là sự kiện bảo mật Critical — thay Log CRUD thường (mục 4.4 spec)
        if (role.IsSystemRole)
        {
            await _auditLog.LogSecurityEventAsync(
                action: "SYSTEM_ROLE_MODIFIED",
                eventCategory: "Security", success: true, severity: "Critical",
                entityName: "Roles",
                entityId: role.Id,
                detail: $"Role hệ thống '{role.Code}' bị sửa",
                oldValue: oldSnapshot,
                newValue: role);

            await TryNotifyAsync(() => _notificationService.NotifySuperAdminsAsync(new NotifyRequest
            {
                Category = NotificationCategories.Security,
                Severity = "Critical",
                Title = "Role hệ thống bị sửa",
                Body = $"Role hệ thống '{role.Code}' ({role.Name}) vừa bị sửa đổi.",
                ActionUrl = "/roles",
                SourceEntityName = "Roles",
                SourceEntityId = role.Id.ToString()
            }));
        }
        else
        {
            _auditLog.Log(
                action: "UPDATE",
                entityName: "Roles",
                entityId: role.Id,
                oldValue: oldSnapshot,
                newValue: role);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        return role;
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var role = await _dbContext.Roles
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (role is null)
        {
            return false;
        }

        var hasAssignments = await _dbContext.UserRoles.AnyAsync(x => x.RoleId == id, cancellationToken);
        if (hasAssignments)
        {
            throw new InvalidOperationException("Cannot delete role because it is assigned to users");
        }

        _auditLog.Log(
            action: "DELETE",
            entityName: "Roles",
            entityId: role.Id,
            oldValue: role,
            newValue: null);

        _dbContext.Roles.Remove(role);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    /// <summary>Lỗi gửi thông báo không được làm hỏng nghiệp vụ sửa role — chỉ log cảnh báo.</summary>
    private async Task TryNotifyAsync(Func<Task> notify)
    {
        try
        {
            await notify();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Không gửi được notification cho sự kiện SYSTEM_ROLE_MODIFIED");
        }
    }
}
