namespace Core.Constants;

/// <summary>
/// Danh sách chính thức của <c>Notifications.Category</c> — tập trung hóa chuỗi category
/// để tránh mỗi nơi gọi tự đặt string khác nhau (bài học từ <c>AuditLogs.Action</c>).
/// </summary>
/// <remarks>
/// Mỗi category gắn với một menu (quyền) tối thiểu để nhận thông báo — "quyền nhận" được
/// phân theo menu RBAC hiện có, không phải user nào cũng nhận được mọi loại. Người nhận
/// broadcast phải nắm menu đó (UserMenus/RoleMenus); SUPER_ADMIN luôn nắm tất cả menu nên
/// luôn nhận. Category không có ánh xạ menu nghĩa là category đó không có đường broadcast
/// theo quyền — chỉ dùng cho thông báo cá nhân (gửi thẳng cho chủ thể).
/// </remarks>
public static class NotificationCategories
{
    /// <summary>Sự kiện bảo mật tài khoản (khóa tài khoản, tái sử dụng refresh token, ...).</summary>
    public const string Security = "Security";

    /// <summary>Sự kiện thay đổi phân quyền (gán/gỡ role, ủy quyền).</summary>
    public const string Permission = "Permission";

    /// <summary>Sự kiện hệ thống chung.</summary>
    public const string System = "System";

    /// <summary>Đánh giá khách hàng (điểm thấp cần admin xử lý).</summary>
    public const string CustomerSatisfaction = "CustomerSatisfaction";

    /// <summary>Danh sách đầy đủ các category chính thức.</summary>
    public static readonly string[] All =
    {
        Security,
        Permission,
        System,
        CustomerSatisfaction
    };

    /// <summary>
    /// Ánh xạ category → menu (quyền) tối thiểu để nhận broadcast của category đó.
    /// Giá trị phải là <c>Menus.Code</c> có trong bảng Menus.
    /// </summary>
    private static readonly IReadOnlyDictionary<string, string> MenuByCategory =
        new Dictionary<string, string>
        {
            // Sự kiện bảo mật tài khoản — chỉ ai quản lý user mới nhận
            [Security] = "USERS",
            // Thay đổi phân quyền (Users/Roles/Menus) nằm trong khu vực quản lý Users
            [Permission] = "USERS",
            // Sự kiện hệ thống — theo dõi nhật ký hệ thống
            [System] = "SYSTEMLOGS",
            [CustomerSatisfaction] = "CUSTOMERSATISFACTION"
        };

    /// <summary>
    /// Trả về menu (quyền) tối thiểu để nhận broadcast theo category. Ném
    /// <see cref="KeyNotFoundException"/> nếu category chưa được khai báo ánh xạ —
    /// category đó không nên gửi broadcast.
    /// </summary>
    public static string GetMenuCode(string category) => MenuByCategory[category];
}