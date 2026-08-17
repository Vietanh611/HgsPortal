using Microsoft.AspNetCore.Components.WebAssembly.Http;

namespace WebApp.Client.Services.Network;

/// <summary>
/// Configures the browser to send cookies with every API request (credentials mode
/// "include") so the HttpOnly refresh-token cookie set by the API is transmitted.
/// </summary>
public class CredentialsHandler : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        // Set credentials mode to include cookies
        request.SetBrowserRequestCredentials(BrowserRequestCredentials.Include);

        return await base.SendAsync(request, cancellationToken);
    }
}
