using System.Security.Claims;
using Animal.API.Authorization;

namespace Animal.API.Services
{
    public sealed class HttpCurrentUser(IHttpContextAccessor httpContextAccessor) : ICurrentUser
    {
        public int UserId
        {
            get
            {
                var value = httpContextAccessor.HttpContext?.User.FindFirstValue(ScopeClaims.LocalUserId);
                return int.TryParse(value, out var userId) && userId > 0
                    ? userId
                    : throw new InvalidOperationException("The authenticated token subject must map to a local user.");
            }
        }
    }
}
