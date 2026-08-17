using Microsoft.AspNetCore.Authentication;

namespace API.Authorization;

/// <summary>
/// Options type for the "DeviceKey" authentication scheme. A distinct type is required by
/// AddScheme&lt;T&gt; to register the handler; no configuration options exist today (the
/// X-Device-Id / X-Device-Key header contract is defined in
/// <see cref="DeviceKeyAuthenticationHandler"/>).
/// </summary>
public class DeviceKeyAuthenticationSchemeOptions : AuthenticationSchemeOptions
{
}
