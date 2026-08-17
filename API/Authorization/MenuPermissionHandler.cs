using Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace API.Authorization;

/// <summary>
/// Evaluates <see cref="MenuPermissionRequirement"/> by comparing the required menu codes of a
/// <see cref="MenuPermissionAttribute"/> against the user's effective menu codes (direct
/// UserMenus + inherited RoleMenus, resolved via <see cref="IMenuService"/> and cached).
/// Menu permissions are deliberately not embedded in the JWT — they are resolved per request so
/// permission changes take effect without re-issuing tokens. Super admins bypass the check, and
/// endpoints with plain [Authorize] (no menu attribute) pass the requirement.
/// </summary>
public class MenuPermissionHandler : AuthorizationHandler<MenuPermissionRequirement>
{
    private readonly IMenuService _menuService;

    public MenuPermissionHandler(IMenuService menuService)
    {
        _menuService = menuService;
    }

    /// <summary>
    /// Grants the requirement when the user is a super admin or holds any of the required menu
    /// codes (case-insensitive match). On mismatch the requirement is left unsatisfied without an
    /// explicit Fail — whether the client receives 401 (challenge) or 403 is decided downstream
    /// by <see cref="MenuAuthorizationResultHandler"/>.
    /// </summary>
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
