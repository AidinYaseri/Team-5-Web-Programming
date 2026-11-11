using Microsoft.AspNetCore.Authentication.Cookies;

namespace Animal.Client.Services
{
    public static class AuthCookieEvents
    {
        // Signs the user out on this device as soon as the access token in their cookie has expired, so the
        // session can't outlive the token (decision D5). The next sign-in is silent while Keycloak's session lasts.
        public static Task RejectIfAccessTokenExpired(CookieValidatePrincipalContext context, TimeProvider timeProvider)
        {
            ArgumentNullException.ThrowIfNull(context);

            if (context.Principal?.Identity?.IsAuthenticated == true
                && AccessTokenLifetime.IsExpired(context.Properties, timeProvider.GetUtcNow()))
            {
                context.RejectPrincipal();
            }

            return Task.CompletedTask;
        }
    }
}
