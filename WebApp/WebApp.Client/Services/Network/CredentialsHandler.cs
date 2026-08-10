using Microsoft.AspNetCore.Components.WebAssembly.Http;

namespace WebApp.Client.Services.Network;

public class CredentialsHandler : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        // Set credentials mode to include cookies
        request.SetBrowserRequestCredentials(BrowserRequestCredentials.Include);

        return await base.SendAsync(request, cancellationToken);
    }
}
