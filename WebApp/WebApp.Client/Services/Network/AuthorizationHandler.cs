using System.Net.Http.Headers;

namespace WebApp.Client.Services.Network;

/// <summary>
/// Attaches the stored access token as a Bearer authorization header to every outgoing
/// request. If the token cannot be read, the request still proceeds unauthenticated so a
/// transient storage failure does not break unrelated API calls.
/// </summary>
public class AuthorizationHandler : DelegatingHandler
{
    private readonly Data.ITokenStorage _tokenStorage;

    public AuthorizationHandler(Data.ITokenStorage tokenStorage)
    {
        _tokenStorage = tokenStorage;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        try
        {
            var token = await _tokenStorage.GetAccessTokenAsync();
            if (!string.IsNullOrEmpty(token))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error adding authorization header: {ex.Message}");
        }

        return await base.SendAsync(request, cancellationToken);
    }
}
