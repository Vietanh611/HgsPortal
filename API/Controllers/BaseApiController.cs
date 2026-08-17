using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

/// <summary>
/// Controller nền cung cấp helper xác thực và URL cho controller dẫn xuất.
/// Kế thừa [IgnoreAntiforgeryToken] ở cấp class — controller dẫn xuất (hiện chỉ UsersController)
/// được miễn kiểm tra CSRF token, khác biệt có chủ đích so với các controller khác của hệ thống.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[IgnoreAntiforgeryToken]
public abstract class BaseApiController : ControllerBase
{
    protected int? CurrentUserId
    {
        get
        {
            var claimValue = User.FindFirst(ClaimTypes.NameIdentifier)?.Value 
                             ?? User.FindFirst("sub")?.Value;

            if (!string.IsNullOrWhiteSpace(claimValue) && int.TryParse(claimValue, out var userId))
            {
                return userId;
            }

            return null;
        }
    }

    protected bool TryGetCurrentUserId(out int userId)
    {
        var id = CurrentUserId;
        if (id.HasValue)
        {
            userId = id.Value;
            return true;
        }

        userId = 0;
        return false;
    }

    protected string? ResolveUrlPath(string? urlPath)
    {
        return UrlPathResolver.Resolve(Request, urlPath);
    }
}
