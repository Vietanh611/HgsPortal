using System.Security.Claims;
using API.Extensions;
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
    protected int? CurrentUserId => User.GetCurrentUserId();

    protected bool TryGetCurrentUserId(out int userId)
    {
        return User.TryGetCurrentUserId(out userId);
    }

    protected string? ResolveUrlPath(string? urlPath)
    {
        return UrlPathResolver.Resolve(Request, urlPath);
    }
}
