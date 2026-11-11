using Microsoft.AspNetCore.Authentication;

namespace Animal.Client.Services
{
    // Reads the signed-in user's access token on the server, from the encrypted auth cookie (SaveTokens = true).
    // The token never goes to the browser: there is no token endpoint and no JavaScript that reads it (decision D4).
    // With interactive server rendering, HttpContext is the request that opened the Blazor connection, which carries
    // the same auth cookie, so this works for both the first render and later interactive calls.
    public sealed class AccessTokenProvider(IHttpContextAccessor httpContextAccessor, TimeProvider timeProvider)
    {
        public async Task<string?> GetTokenAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var context = httpContextAccessor.HttpContext;
            if (context is null)
            {
                return null;
            }

            var result = await context.AuthenticateAsync();
            if (!result.Succeeded)
            {
                return null;
            }

            // An expired token would only get a 401 from the API. Treat it as signed out instead.
            if (AccessTokenLifetime.IsExpired(result.Properties, timeProvider.GetUtcNow()))
            {
                return null;
            }

            var token = result.Properties?.GetTokenValue("access_token");
            return string.IsNullOrWhiteSpace(token) ? null : token;
        }
    }
}
