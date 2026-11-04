using System.Security.Claims;

namespace Animal.API.Authorization;

public static class ScopeClaims
{
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
}
