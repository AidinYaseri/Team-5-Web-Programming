using System.Net.Http.Headers;

namespace Animal.Client.Services
{
    // Adds the signed-in user's access token to calls to Animal.API. The token comes from the server-side
    // auth cookie only (AccessTokenProvider); requests from signed-out users go out without one.
    public sealed class AccessTokenHandler(AccessTokenProvider accessTokenProvider) : DelegatingHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var token = await accessTokenProvider.GetTokenAsync(cancellationToken);
            if (!string.IsNullOrWhiteSpace(token))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }

            return await base.SendAsync(request, cancellationToken);
        }
    }
}
