namespace API.Authorization;

/// <summary>
/// Declares the menu code(s) required to access a controller or action. It only marks the
/// endpoint — enforcement is delegated to <see cref="MenuPermissionHandler"/>, which reads the
/// attribute from endpoint metadata.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
public sealed class MenuPermissionAttribute : Attribute
{
    /// <summary>The menu code(s) the caller must hold; matching any single code is sufficient.</summary>
    public string[] Codes { get; }

    public MenuPermissionAttribute(params string[] codes) => Codes = codes;
}
