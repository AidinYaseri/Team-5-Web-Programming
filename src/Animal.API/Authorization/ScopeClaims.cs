using System.Security.Claims;

namespace Animal.API.Authorization
{
    public static class ScopeClaims
    {
        public const string Subject = "sub";

        // Added by SubjectToUserIdClaimsTransformation after looking up the token's sub. Never taken from the token.
        public const string LocalUserId = "local_user_id";

        // Scopes arrive as one space-separated string, in "scope" (most providers) or "scp" (Entra ID). Exact match.
        public static bool HasScope(ClaimsPrincipal user, string requiredScope)
        {
            ArgumentNullException.ThrowIfNull(user);
            ArgumentException.ThrowIfNullOrWhiteSpace(requiredScope);

            return user.Claims
                .Where(claim => claim.Type is "scope" or "scp")
                .SelectMany(claim => claim.Value.Split(
                    (char[]?)null,
                    StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                .Contains(requiredScope, StringComparer.Ordinal);
        }

        public static bool HasMappedUser(ClaimsPrincipal user)
        {
            ArgumentNullException.ThrowIfNull(user);

            return int.TryParse(user.FindFirst(LocalUserId)?.Value, out var userId) && userId > 0;
        }
    }
}
