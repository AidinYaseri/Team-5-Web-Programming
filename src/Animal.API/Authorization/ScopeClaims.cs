using System.Security.Claims;

namespace Animal.API.Authorization
{
    public static class ScopeClaims
    {
        public const string Subject = "sub";

        // Added by SubjectToUserIdClaimsTransformation after looking up the token's sub. Never taken from the token.
        public const string LocalUserId = "local_user_id";

        // Realm roles from the validated access token. Keycloak's realm roles mapper writes them as a flat "roles" array
        // (decision D2 in docs/milestone-4/decisions.md), so with MapInboundClaims = false each role is its own claim.
        public const string Roles = "roles";

        // Shelter staff. Needed on top of the scope for staff operations (decision D3).
        public const string StaffRole = "staff";

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

        // Exact, case-sensitive match on one "roles" claim. Nested claims such as Keycloak's realm_access are not read.
        public static bool HasRole(ClaimsPrincipal user, string requiredRole)
        {
            ArgumentNullException.ThrowIfNull(user);
            ArgumentException.ThrowIfNullOrWhiteSpace(requiredRole);

            return user.Claims.Any(claim => claim.Type == Roles && string.Equals(claim.Value, requiredRole, StringComparison.Ordinal));
        }

        public static bool HasMappedUser(ClaimsPrincipal user)
        {
            ArgumentNullException.ThrowIfNull(user);

            return int.TryParse(user.FindFirst(LocalUserId)?.Value, out var userId) && userId > 0;
        }
    }
}
