using System.Security.Claims;

namespace API.Extensions;

/// <summary>
/// Helper lấy userId của caller từ JWT claims — dùng chung cho mọi controller
/// phục vụ dữ liệu của user đang đăng nhập (my-menus, my-roles, notifications, auth/me).
/// Tách thành extension để tránh kế thừa BaseApiController vốn gắn [IgnoreAntiforgeryToken] ở cấp class.
/// </summary>
public static class ClaimsPrincipalExtensions
{
    public static int? GetCurrentUserId(this ClaimsPrincipal user)
    {
        var claimValue = user.FindFirst(ClaimTypes.NameIdentifier)?.Value
                         ?? user.FindFirst("sub")?.Value;

        if (!string.IsNullOrWhiteSpace(claimValue) && int.TryParse(claimValue, out var userId))
        {
            return userId;
        }

        return null;
    }

    public static bool TryGetCurrentUserId(this ClaimsPrincipal user, out int userId)
    {
        var id = user.GetCurrentUserId();
        if (id.HasValue)
        {
            userId = id.Value;
            return true;
        }

        userId = 0;
        return false;
    }
}