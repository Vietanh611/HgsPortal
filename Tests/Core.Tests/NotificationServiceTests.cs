using Core.Constants;
using Core.Services.Notifications;
using Core.Tests.Fakes;
using Data.DbContexts;
using Domain.Entities.Identity;
using Domain.Entities.System;
using Hgs.Share.Requests.Notifications;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Core.Tests;

/// <summary>
/// Test NotificationService với EF Core InMemory. Bao phủ luồng tạo thông báo (recipients,
/// dedup), phân quyền người nhận (menu code, super admin, resolve category→menu, lọc theo
/// org scope), đọc (pagination/filter), đánh dấu đã đọc (kèm chống IDOR).
/// </summary>
public class NotificationServiceTests
{
    private readonly FakeMenuService _menuService = new();
    private readonly NullLogger<NotificationService> _logger = NullLogger<NotificationService>.Instance;

    private (HgsDbContext db, NotificationService service) CreateService(string dbName)
    {
        var options = new DbContextOptionsBuilder<HgsDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        var db = new HgsDbContext(options);
        var service = new NotificationService(db, _menuService, _logger);
        return (db, service);
    }

    private static NotifyRequest NewRequest(string category, string title = "Thông báo")
    {
        return new NotifyRequest
        {
            Category = category,
            Severity = "Info",
            Title = title,
            Body = "Nội dung chi tiết",
            ActionUrl = "/audit",
            TriggeredByUserId = 1
        };
    }

    private static Users NewUser(string username, int id, int orgUnitId = 1)
    {
        return new Users
        {
            Id = id,
            Username = username,
            Email = $"{username}@example.com",
            PasswordHash = "hash",
            FullName = username,
            OrganizationUnitId = orgUnitId,
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };
    }

    private static OrganizationUnits NewOrg(int id, string code, string path, int level)
    {
        return new OrganizationUnits
        {
            Id = id,
            Code = code,
            Name = code,
            Path = path,
            Level = level,
            SortOrder = 0,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
    }

    [Fact]
    public async Task NotifyUsersAsync_CreatesNotificationAndRecipientRows()
    {
        var (db, service) = CreateService(nameof(NotifyUsersAsync_CreatesNotificationAndRecipientRows));

        await service.NotifyUsersAsync(NewRequest(NotificationCategories.Security), new[] { 10, 20 });

        var notification = await db.Notifications.SingleAsync();
        var recipients = await db.NotificationRecipients.ToListAsync();

        Assert.Equal(NotificationCategories.Security, notification.Category);
        Assert.Equal(2, recipients.Count);
        Assert.Contains(recipients, r => r.UserId == 10);
        Assert.Contains(recipients, r => r.UserId == 20);
        Assert.True(notification.ExpiresAt > DateTime.UtcNow.AddDays(29));
    }

    [Fact]
    public async Task NotifyUsersAsync_DeduplicatesUserIds()
    {
        var (db, service) = CreateService(nameof(NotifyUsersAsync_DeduplicatesUserIds));

        await service.NotifyUsersAsync(NewRequest(NotificationCategories.System), new[] { 10, 10, 20, 20 });

        var recipients = await db.NotificationRecipients.ToListAsync();
        Assert.Equal(2, recipients.Count);
    }

    [Fact]
    public async Task NotifyUsersAsync_EmptyUserIds_CreatesNothing()
    {
        var (db, service) = CreateService(nameof(NotifyUsersAsync_EmptyUserIds_CreatesNothing));

        await service.NotifyUsersAsync(NewRequest(NotificationCategories.System), Array.Empty<int>());

        Assert.Equal(0, await db.Notifications.CountAsync());
        Assert.Equal(0, await db.NotificationRecipients.CountAsync());
    }

    [Fact]
    public async Task NotifyByMenuPermissionAsync_NotifiesUsersWithMenuCode()
    {
        var (db, service) = CreateService(nameof(NotifyByMenuPermissionAsync_NotifiesUsersWithMenuCode));
        _menuService.UserIdsWithMenuCode.AddRange(new[] { 1, 2, 3 });

        await service.NotifyByMenuPermissionAsync(NewRequest(NotificationCategories.Permission), "USERS");

        var recipients = await db.NotificationRecipients.ToListAsync();
        Assert.Equal(3, recipients.Count);
    }

    [Fact]
    public async Task NotifyByCategoryAsync_ResolvesCategoryMenuCode()
    {
        var (db, service) = CreateService(nameof(NotifyByCategoryAsync_ResolvesCategoryMenuCode));
        _menuService.UserIdsWithMenuCode.AddRange(new[] { 1, 2 });

        // Security phải resolve sang menu "USERS" (ánh xạ trong NotificationCategories)
        await service.NotifyByCategoryAsync(NewRequest(NotificationCategories.Security));

        Assert.Equal("USERS", _menuService.LastMenuCode);
        var recipients = await db.NotificationRecipients.ToListAsync();
        Assert.Equal(2, recipients.Count);
    }

    [Fact]
    public async Task NotifyByCategoryAsync_UnknownCategory_Throws()
    {
        var (db, service) = CreateService(nameof(NotifyByCategoryAsync_UnknownCategory_Throws));

        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => service.NotifyByCategoryAsync(NewRequest("UnknownCategory")));

        Assert.Empty(await db.Notifications.ToListAsync());
    }

    [Fact]
    public async Task NotifyByCategoryAsync_OrgScope_FiltersToUsersCoveringOrg()
    {
        var (db, service) = CreateService(nameof(NotifyByCategoryAsync_OrgScope_FiltersToUsersCoveringOrg));
        db.OrganizationUnits.AddRange(
            NewOrg(1, "HGS", "1", 0),
            NewOrg(5, "CN-TSN", "1/5", 1),
            NewOrg(9, "CN-DAD", "9", 0));
        db.Users.AddRange(
            NewUser("in-child-org", 1, orgUnitId: 5),
            NewUser("in-root-org", 2, orgUnitId: 1),
            NewUser("in-sibling-org", 3, orgUnitId: 9));
        await db.SaveChangesAsync();
        _menuService.UserIdsWithMenuCode.AddRange(new[] { 1, 2, 3 });

        // Sự kiện xảy ra tại org 5 (path "1/5"): user 1 (org 5) và user 2 (org 1 = tổ tiên)
        // trong phạm vi; user 3 (org 9 ngoài phạm vi) bị loại
        await service.NotifyByCategoryAsync(NewRequest(NotificationCategories.CustomerSatisfaction), orgUnitId: 5);

        var recipients = await db.NotificationRecipients.ToListAsync();
        Assert.Equal(2, recipients.Count);
        Assert.Contains(recipients, r => r.UserId == 1);
        Assert.Contains(recipients, r => r.UserId == 2);
        Assert.DoesNotContain(recipients, r => r.UserId == 3);
    }

    [Fact]
    public async Task NotifyByCategoryAsync_OrgScope_UnknownOrgUnit_FallsBackToAll()
    {
        var (db, service) = CreateService(nameof(NotifyByCategoryAsync_OrgScope_UnknownOrgUnit_FallsBackToAll));
        _menuService.UserIdsWithMenuCode.AddRange(new[] { 1, 2, 3 });

        // Org 999 không tồn tại → không xác định được phạm vi, gửi cho toàn bộ user có menu
        await service.NotifyByCategoryAsync(NewRequest(NotificationCategories.CustomerSatisfaction), orgUnitId: 999);

        var recipients = await db.NotificationRecipients.ToListAsync();
        Assert.Equal(3, recipients.Count);
    }

    [Fact]
    public async Task NotifyByCategoryAsync_OrgScope_SuperAdminAlwaysIncluded()
    {
        var (db, service) = CreateService(nameof(NotifyByCategoryAsync_OrgScope_SuperAdminAlwaysIncluded));
        db.OrganizationUnits.AddRange(
            NewOrg(1, "HGS", "1", 0),
            NewOrg(5, "CN-TSN", "1/5", 1),
            NewOrg(9, "CN-DAD", "9", 0));
        db.Users.AddRange(
            NewUser("in-child-org", 1, orgUnitId: 5),
            NewUser("in-root-org", 2, orgUnitId: 1),
            NewUser("superadmin-out-of-scope", 3, orgUnitId: 9));
        var superAdminRole = new Roles
        {
            Code = RoleCodes.SuperAdmin,
            Name = "Super Admin",
            IsSystemRole = true,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        db.Roles.Add(superAdminRole);
        await db.SaveChangesAsync();
        db.UserRoles.Add(new UserRoles { UserId = 3, RoleId = superAdminRole.Id, AssignedAt = DateTime.UtcNow });
        await db.SaveChangesAsync();
        _menuService.UserIdsWithMenuCode.AddRange(new[] { 1, 2, 3 });

        // SUPER_ADMIN giữ vai trò toàn cục nên vẫn nhận dù org ngoài phạm vi sự kiện
        await service.NotifyByCategoryAsync(NewRequest(NotificationCategories.CustomerSatisfaction), orgUnitId: 5);

        var recipients = await db.NotificationRecipients.ToListAsync();
        Assert.Equal(3, recipients.Count);
        Assert.Contains(recipients, r => r.UserId == 3);
    }

    [Fact]
    public async Task NotifySuperAdminsAsync_NotifiesOnlyActiveSuperAdminRoleUsers()
    {
        var (db, service) = CreateService(nameof(NotifySuperAdminsAsync_NotifiesOnlyActiveSuperAdminRoleUsers));
        db.OrganizationUnits.Add(new OrganizationUnits
        {
            Id = 1,
            Code = "HGS",
            Name = "HGS",
            Level = 0,
            SortOrder = 0,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        });
        db.Users.AddRange(NewUser("admin", 1), NewUser("inactive", 2), NewUser("normal", 3));
        var superAdminRole = new Roles
        {
            Code = RoleCodes.SuperAdmin,
            Name = "Super Admin",
            IsSystemRole = true,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        var inactiveRole = new Roles
        {
            Code = RoleCodes.SuperAdmin,
            Name = "Super Admin (disabled)",
            IsSystemRole = true,
            IsActive = false,
            CreatedAt = DateTime.UtcNow
        };
        db.Roles.AddRange(superAdminRole, inactiveRole);
        await db.SaveChangesAsync();
        db.UserRoles.AddRange(
            new UserRoles { UserId = 1, RoleId = superAdminRole.Id, AssignedAt = DateTime.UtcNow },
            new UserRoles { UserId = 2, RoleId = inactiveRole.Id, AssignedAt = DateTime.UtcNow },
            new UserRoles { UserId = 3, RoleId = 99, AssignedAt = DateTime.UtcNow });
        await db.SaveChangesAsync();

        await service.NotifySuperAdminsAsync(NewRequest(NotificationCategories.Security));

        var recipients = await db.NotificationRecipients.ToListAsync();
        var recipient = Assert.Single(recipients);
        Assert.Equal(1, recipient.UserId);
    }

    [Fact]
    public async Task GetMyNotificationsAsync_PaginatesAndFilters()
    {
        var (db, service) = CreateService(nameof(GetMyNotificationsAsync_PaginatesAndFilters));
        for (var i = 1; i <= 5; i++)
        {
            db.Notifications.Add(new Notifications
            {
                Category = i % 2 == 0 ? NotificationCategories.System : NotificationCategories.Security,
                Severity = "Info",
                Title = $"Tin {i}",
                CreatedAt = DateTime.UtcNow.AddMinutes(-i),
                ExpiresAt = DateTime.UtcNow.AddDays(30)
            });
        }
        await db.SaveChangesAsync();

        var ids = await db.Notifications.OrderBy(n => n.CreatedAt).Select(n => n.Id).ToListAsync();
        for (var i = 0; i < ids.Count; i++)
        {
            // i (index) chẵn = Security + chưa đọc; i lẻ = System + đã đọc
            db.NotificationRecipients.Add(new NotificationRecipients
            {
                NotificationId = ids[i],
                UserId = 10,
                IsRead = i % 2 == 1
            });
        }
        await db.SaveChangesAsync();

        // Trang 1, lọc chỉ chưa đọc, category Security
        var page1 = await service.GetMyNotificationsAsync(10, new NotificationFilterRequest
        {
            PageNumber = 1,
            PageSize = 2,
            Category = NotificationCategories.Security,
            IsRead = false
        });

        Assert.Equal(2, page1.Items.Count());
        Assert.Equal(3, page1.TotalCount);
        Assert.Equal(2, page1.TotalPages);
        Assert.All(page1.Items, item =>
        {
            Assert.Equal(NotificationCategories.Security, item.Category);
            Assert.False(item.IsRead);
        });

        // Mặc định tất cả dòng, trang 2
        var page2 = await service.GetMyNotificationsAsync(10, new NotificationFilterRequest { PageNumber = 2, PageSize = 2 });
        Assert.Equal(2, page2.Items.Count());
        Assert.Equal(5, page2.TotalCount);
        Assert.Equal(3, page2.TotalPages);
        Assert.Equal("Tin 3", page2.Items.First().Title); // sắp theo CreatedAt giảm dần
    }

    [Fact]
    public async Task GetUnreadCountAsync_CountsOnlyUnread()
    {
        var (db, service) = CreateService(nameof(GetUnreadCountAsync_CountsOnlyUnread));
        db.Notifications.Add(new Notifications
        {
            Category = NotificationCategories.System,
            Severity = "Info",
            Title = "T1",
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddDays(30)
        });
        await db.SaveChangesAsync();
        var id = await db.Notifications.Select(n => n.Id).SingleAsync();
        db.NotificationRecipients.AddRange(
            new NotificationRecipients { NotificationId = id, UserId = 10, IsRead = false },
            new NotificationRecipients { NotificationId = id, UserId = 10, IsRead = true },
            new NotificationRecipients { NotificationId = id, UserId = 20, IsRead = false });
        await db.SaveChangesAsync();

        Assert.Equal(1, await service.GetUnreadCountAsync(10));
        Assert.Equal(1, await service.GetUnreadCountAsync(20));
    }

    [Fact]
    public async Task MarkAsReadAsync_OnlyAffectsOwnRecipient()
    {
        var (db, service) = CreateService(nameof(MarkAsReadAsync_OnlyAffectsOwnRecipient));
        db.Notifications.Add(new Notifications
        {
            Category = NotificationCategories.System,
            Severity = "Info",
            Title = "T1",
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddDays(30)
        });
        await db.SaveChangesAsync();
        var id = await db.Notifications.Select(n => n.Id).SingleAsync();
        db.NotificationRecipients.AddRange(
            new NotificationRecipients { NotificationId = id, UserId = 10, IsRead = false },
            new NotificationRecipients { NotificationId = id, UserId = 20, IsRead = false });
        await db.SaveChangesAsync();

        // User 20 cố đánh dấu đã đọc thông báo của user 10 — IDOR phải bị chặn
        await service.MarkAsReadAsync(20, id);
        db.ChangeTracker.Clear();

        var r10 = await db.NotificationRecipients.SingleAsync(r => r.UserId == 10);
        var r20 = await db.NotificationRecipients.SingleAsync(r => r.UserId == 20);
        Assert.False(r10.IsRead);
        Assert.True(r20.IsRead);
    }

    [Fact]
    public async Task MarkAllAsReadAsync_MarksAllUnread()
    {
        var (db, service) = CreateService(nameof(MarkAllAsReadAsync_MarksAllUnread));
        db.Notifications.Add(new Notifications
        {
            Category = NotificationCategories.System,
            Severity = "Info",
            Title = "T1",
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddDays(30)
        });
        await db.SaveChangesAsync();
        var id = await db.Notifications.Select(n => n.Id).SingleAsync();
        db.NotificationRecipients.AddRange(
            new NotificationRecipients { NotificationId = id, UserId = 10, IsRead = false },
            new NotificationRecipients { NotificationId = id, UserId = 10, IsRead = false },
            new NotificationRecipients { NotificationId = id, UserId = 20, IsRead = false });
        await db.SaveChangesAsync();

        await service.MarkAllAsReadAsync(10);
        db.ChangeTracker.Clear();

        Assert.Equal(0, await db.NotificationRecipients.CountAsync(r => r.UserId == 10 && !r.IsRead));
        Assert.Equal(1, await db.NotificationRecipients.CountAsync(r => r.UserId == 20 && !r.IsRead));
    }
}