using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

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
