using Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace API.Authorization;

public class MenuPermissionHandler : AuthorizationHandler<MenuPermissionRequirement>
{
    private readonly IMenuService _menuService;

    public MenuPermissionHandler(IMenuService menuService)
    {
        _menuService = menuService;
    }

    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, MenuPermissionRequirement requirement)
    {
        if (context.Resource is not HttpContext httpContext)
        {
            return;
        }

        var endpoint = httpContext.GetEndpoint();
        if (endpoint is null)
        {
            return;
        }

        var menuPermission = endpoint.Metadata.GetMetadata<MenuPermissionAttribute>();
        if (menuPermission is null)
        {
            if (endpoint.Metadata.GetMetadata<IAuthorizeData>() is not null)
            {
                context.Succeed(requirement);
            }

            return;
        }

        var userIdValue = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userIdValue, out var userId))
        {
            return;
        }

        if (await _menuService.IsSuperAdminAsync(userId))
        {
            context.Succeed(requirement);
            return;
        }

        var effectiveCodes = await _menuService.GetEffectiveMenuCodesAsync(userId);

        if (menuPermission.Codes.Any(code => effectiveCodes.Contains(code)))
        {
            context.Succeed(requirement);
        }
    }
}
