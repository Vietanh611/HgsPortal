using Microsoft.AspNetCore.Components.Authorization;
using System.Security.Claims;

namespace WebApp.Services;

/// <summary>
/// Server-side implementation of AuthenticationStateProvider for prerendering.
/// Always returns unauthenticated state since tokens are stored on the client side.
/// </summary>
public class ServerAuthenticationStateProvider : AuthenticationStateProvider
{
    public override Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        var anonymous = new ClaimsPrincipal(new ClaimsIdentity());
        return Task.FromResult(new AuthenticationState(anonymous));
    }
}
