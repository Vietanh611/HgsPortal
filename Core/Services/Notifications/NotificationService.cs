using Core.Constants;
using Core.Interfaces.Identity;
using Core.Interfaces.Notifications;
using Data.DbContexts;
using Domain.Entities.System;
using Hgs.Share.Requests.Notifications;
using Hgs.Share.Responses.ApiResponses;
using Hgs.Share.Responses.Notifications;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Core.Services.Notifications;

public class NotificationService : INotificationService
{
    private readonly HgsDbContext _dbContext;
    private readonly IMenuService _menuService;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(
        HgsDbContext dbContext,
        IMenuService menuService,
        ILogger<NotificationService> logger)
    {
        _dbContext = dbContext;
        _menuService = menuService;
        _logger = logger;
    }

    public async Task NotifyUsersAsync(NotifyRequest request, IEnumerable<int> userIds, CancellationToken cancellationToken = default)
    {
        var distinctUserIds = userIds.Distinct().ToList();
        if (distinctUserIds.Count == 0)
        {
            return;
        }

        // Tên class trùng namespace file (Core.Services.Notifications) nên phải dùng tên đủ
        // domain: Domain.Entities.System.Notifications
        var notification = new Domain.Entities.System.Notifications
        {
            Category = request.Category,
            Severity = request.Severity,
            Title = request.Title,
            Body = request.Body,
            ActionUrl = request.ActionUrl,
            SourceEntityName = request.SourceEntityName,
            SourceEntityId = request.SourceEntityId,
            TriggeredByUserId = request.TriggeredByUserId,
            CorrelationId = request.CorrelationId,
            CreatedAt = DateTime.UtcNow,
            // Mặc định giữ 30 ngày; job dọn dẹp xóa cứng sau mốc này
            ExpiresAt = DateTime.UtcNow.AddDays(30)
        };

        _dbContext.Notifications.Add(notification);
        await _dbContext.SaveChangesAsync(cancellationToken);

        // "Quyền nhận" đã được quyết định ở nơi gọi (danh sách userId, menu, hay super admin)
        // — không còn bộ lọc preference riêng của user. User được liệt kê thì luôn nhận.
        var recipients = distinctUserIds
            .Select(uid => new NotificationRecipients { NotificationId = notification.Id, UserId = uid })
            .ToList();

        _dbContext.NotificationRecipients.AddRange(recipients);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task NotifyByMenuPermissionAsync(NotifyRequest request, string menuCode, CancellationToken cancellationToken = default)
    {
        var userIds = await _menuService.GetUserIdsWithMenuCodeAsync(menuCode, cancellationToken);
        await NotifyUsersAsync(request, userIds, cancellationToken);
    }

    public async Task NotifyByCategoryAsync(NotifyRequest request, int? orgUnitId = null, CancellationToken cancellationToken = default)
    {
        var menuCode = NotificationCategories.GetMenuCode(request.Category);
        var userIds = await _menuService.GetUserIdsWithMenuCodeAsync(menuCode, cancellationToken);

        // Sự kiện gắn với một org cụ thể thì chỉ user quản lý org đó (bằng hoặc cấp cha
        // theo Path) được nhận; không resolve được org thì fallback về toàn bộ user có menu.
        if (orgUnitId is int orgId)
        {
            userIds = await FilterToOrgScopeAsync(userIds, orgId, cancellationToken);
        }

        await NotifyUsersAsync(request, userIds, cancellationToken);
    }

    /// <summary>Lọc danh sách user về những người quản lý orgUnitId: org của user bằng hoặc là tổ tiên (path prefix) của org phát sinh; SUPER_ADMIN luôn được giữ.</summary>
    private async Task<List<int>> FilterToOrgScopeAsync(IEnumerable<int> userIds, int orgUnitId, CancellationToken cancellationToken)
    {
        var sourceOrgPath = await _dbContext.OrganizationUnits
            .AsNoTracking()
            .Where(o => o.Id == orgUnitId)
            .Select(o => o.Path)
            .FirstOrDefaultAsync(cancellationToken);

        // Org không tồn tại → không thể xác định phạm vi, fallback về toàn bộ người nhận menu
        if (string.IsNullOrEmpty(sourceOrgPath))
        {
            return userIds.ToList();
        }

        var candidateIds = userIds.ToList();

        // SUPER_ADMIN nắm toàn bộ phạm vi (ngữ nghĩa giống GetUserIdsWithMenuCodeAsync) nên
        // được giữ kể cả khi org của họ ngoài phạm vi sự kiện
        var superAdminIds = await _dbContext.UserRoles
            .AsNoTracking()
            .Where(x => candidateIds.Contains(x.UserId)
                && x.Role.Code == RoleCodes.SuperAdmin
                && x.Role.IsActive)
            .Select(x => x.UserId)
            .ToListAsync(cancellationToken);

        // User quản lý org phát sinh khi org của họ bằng hoặc là tổ tiên (path prefix của
        // org phát sinh). User không gắn org (OrganizationUnit == null) ngoài phạm vi trừ khi là SUPER_ADMIN.
        var inScopeIds = await _dbContext.Users
            .AsNoTracking()
            .Where(u => candidateIds.Contains(u.Id)
                && u.OrganizationUnit != null
                && (u.OrganizationUnit.Path == sourceOrgPath
                    || (u.OrganizationUnit.Path != null && sourceOrgPath.StartsWith(u.OrganizationUnit.Path + "/"))))
            .Select(u => u.Id)
            .ToListAsync(cancellationToken);

        return inScopeIds.Union(superAdminIds).ToList();
    }

    public async Task NotifySuperAdminsAsync(NotifyRequest request, CancellationToken cancellationToken = default)
    {
        var superAdminUserIds = await _dbContext.UserRoles
            .AsNoTracking()
            .Where(x => x.Role.Code == RoleCodes.SuperAdmin && x.Role.IsActive)
            .Select(x => x.UserId)
            .ToListAsync(cancellationToken);

        await NotifyUsersAsync(request, superAdminUserIds, cancellationToken);
    }

    public async Task<PagedResponse<NotificationListItemResponse>> GetMyNotificationsAsync(
        int userId,
        NotificationFilterRequest filter,
        CancellationToken cancellationToken = default)
    {
        var pageNumber = Math.Max(1, filter.PageNumber);
        var pageSize = Math.Clamp(filter.PageSize < 1 ? 20 : filter.PageSize, 1, 200);

        var query = _dbContext.NotificationRecipients
            .AsNoTracking()
            .Where(r => r.UserId == userId);

        if (!string.IsNullOrWhiteSpace(filter.Category))
        {
            query = query.Where(r => r.Notification.Category == filter.Category);
        }

        if (filter.IsRead.HasValue)
        {
            query = query.Where(r => r.IsRead == filter.IsRead.Value);
        }

        var total = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(r => r.Notification.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(r => new NotificationListItemResponse
            {
                Id = r.NotificationId,
                Category = r.Notification.Category,
                Severity = r.Notification.Severity,
                Title = r.Notification.Title,
                Body = r.Notification.Body,
                ActionUrl = r.Notification.ActionUrl,
                SourceEntityName = r.Notification.SourceEntityName,
                SourceEntityId = r.Notification.SourceEntityId,
                IsRead = r.IsRead,
                CreatedAt = r.Notification.CreatedAt
            })
            .ToListAsync(cancellationToken);

        return new PagedResponse<NotificationListItemResponse>
        {
            Items = items,
            TotalCount = total,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalPages = (int)Math.Ceiling((double)total / pageSize)
        };
    }

    public async Task<int> GetUnreadCountAsync(int userId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.NotificationRecipients
            .AsNoTracking()
            .CountAsync(r => r.UserId == userId && !r.IsRead, cancellationToken);
    }

    public async Task MarkAsReadAsync(int userId, long notificationId, CancellationToken cancellationToken = default)
    {
        // Query luôn điều kiện theo UserId (từ ClaimsPrincipal) — không tìm theo Id rồi check
        // sau, để chặn IDOR ngay từ truy vấn.
        var recipient = await _dbContext.NotificationRecipients
            .FirstOrDefaultAsync(r => r.NotificationId == notificationId && r.UserId == userId, cancellationToken);

        if (recipient is null)
        {
            return;
        }

        if (!recipient.IsRead)
        {
            recipient.IsRead = true;
            recipient.ReadAt = DateTime.UtcNow;
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task MarkAllAsReadAsync(int userId, CancellationToken cancellationToken = default)
    {
        var unread = await _dbContext.NotificationRecipients
            .Where(r => r.UserId == userId && !r.IsRead)
            .ToListAsync(cancellationToken);

        if (unread.Count == 0)
        {
            return;
        }

        var now = DateTime.UtcNow;
        foreach (var recipient in unread)
        {
            recipient.IsRead = true;
            recipient.ReadAt = now;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}