using System.Security.Claims;

namespace Animal.API.Services
{
    // Authentication must map the signed-in account to the local user row and emit this trusted numeric claim.
    public sealed class CurrentUser(IHttpContextAccessor httpContextAccessor) : ICurrentUser
    {
        public int UserId
        {
            get
            {
                var value = httpContextAccessor.HttpContext?.User.FindFirstValue("user_id");
                return int.TryParse(value, out var userId) && userId > 0
                    ? userId
                    : throw new InvalidOperationException("The authenticated token must contain a valid user_id claim.");
            }
        }
    }
}
