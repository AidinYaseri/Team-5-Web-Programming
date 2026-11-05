using System.Security.Claims;

namespace Animal.API.Authorization
{
    public static class ScopeClaims
    {
        public const string Subject = "sub";
        public const string LocalUserId = "local_user_id";

        public static bool HasScope(ClaimsPrincipal user, string requiredScope) =>
            user.FindAll("scope").Concat(user.FindAll("scp"))
                .SelectMany(claim => claim.Value.Split(' ', StringSplitOptions.RemoveEmptyEntries))
                .Contains(requiredScope, StringComparer.Ordinal);

        public static bool HasMappedUser(ClaimsPrincipal user) =>
            int.TryParse(user.FindFirst(LocalUserId)?.Value, out var userId) && userId > 0;
    }
}
