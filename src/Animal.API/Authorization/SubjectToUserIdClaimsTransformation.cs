using System.Security.Claims;
using Animal.Data.Repositories;
using Microsoft.AspNetCore.Authentication;

namespace Animal.API.Authorization
{
    // Providers send an opaque subject, not our database's integer key. Resolve that subject to a local user row.
    public sealed class SubjectToUserIdClaimsTransformation(IUserRepository users) : IClaimsTransformation
    {
        public async Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
        {
            // Never trust a local database ID supplied by the external identity provider.
            foreach (var identity in principal.Identities)
            {
                foreach (var claim in identity.FindAll(ScopeClaims.LocalUserId).ToList())
                {
                    identity.RemoveClaim(claim);
                }
            }

            var subject = principal.FindFirst(ScopeClaims.Subject)?.Value;
            if (string.IsNullOrWhiteSpace(subject))
            {
                return principal;
            }

            var user = await users.GetByExternalIdAsync(subject);
            if (user is null)
            {
                return principal;
            }

            var subjectIdentity = principal.Identities.FirstOrDefault(candidate => candidate.HasClaim(ScopeClaims.Subject, subject));
            if (subjectIdentity is not null)
            {
                subjectIdentity.AddClaim(new Claim(ScopeClaims.LocalUserId, user.Id.ToString(System.Globalization.CultureInfo.InvariantCulture)));
            }

            return principal;
        }
    }
}
