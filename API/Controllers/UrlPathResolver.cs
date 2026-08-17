namespace API.Controllers;

public static class UrlPathResolver
{
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