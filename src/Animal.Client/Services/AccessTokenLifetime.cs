using System.Globalization;
using Microsoft.AspNetCore.Authentication;

namespace Animal.Client.Services
{
    // The OIDC handler saves the access token's expiry as "expires_at" next to the tokens in the auth cookie.
    public static class AccessTokenLifetime
    {
        public static bool IsExpired(AuthenticationProperties? properties, DateTimeOffset now)
        {
            var expiresAt = properties?.GetTokenValue("expires_at");
            if (string.IsNullOrWhiteSpace(expiresAt))
            {
                // No expiry saved (for example no token at all): nothing to use, so count it as expired.
                return true;
            }

            return !DateTimeOffset.TryParse(expiresAt, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var expiry)
                || expiry <= now;
        }
    }
}
