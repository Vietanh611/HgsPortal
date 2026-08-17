namespace API.Controllers;

/// <summary>
/// Resolves URL paths stored in the database (relative to the API root, e.g. avatar or asset
/// paths) to absolute URLs for the current request, so clients render links correctly
/// regardless of the host they are served from.
/// </summary>
public static class UrlPathResolver
{
    /// <summary>
    /// Returns the path unchanged when it is already absolute, empty, or not root-relative; only
    /// root-relative paths ("/...") are prefixed with the request's scheme/host/PathBase.
    /// </summary>
    public static string? Resolve(HttpRequest request, string? urlPath)
    {
        if (string.IsNullOrWhiteSpace(urlPath))
        {
            return urlPath;
        }

        if (Uri.TryCreate(urlPath, UriKind.Absolute, out _))
        {
            return urlPath;
        }

        if (!urlPath.StartsWith("/", StringComparison.Ordinal))
        {
            return urlPath;
        }

        return $"{request.Scheme}://{request.Host}{request.PathBase}{urlPath}";
    }
}