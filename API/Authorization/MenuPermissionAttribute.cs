namespace API.Authorization;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
public sealed class MenuPermissionAttribute : Attribute
{
    public string[] Codes { get; }

    public MenuPermissionAttribute(params string[] codes) => Codes = codes;
}
