using Microsoft.AspNetCore.Authorization;

namespace API.Authorization;

/// <summary>
/// Marker requirement for menu-based RBAC, applied to every endpoint through the fallback policy
/// in Program.cs. All evaluation logic lives in <see cref="MenuPermissionHandler"/>.
/// </summary>
public class MenuPermissionRequirement : IAuthorizationRequirement
{
}
