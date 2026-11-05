using System.Globalization;
using System.Security.Claims;
using Animal.API.Authorization;

namespace Animal.API.Services
{
    public sealed class HttpCurrentUser : ICurrentUser
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public HttpCurrentUser(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public string Subject
        {
            get
            {
                var subject = _httpContextAccessor.HttpContext?.User.FindFirstValue(ScopeClaims.Subject);
                return string.IsNullOrWhiteSpace(subject)
                    ? throw new InvalidOperationException("The authenticated user is missing the required 'sub' claim.")
                    : subject;
            }
        }

        public int UserId
        {
            get
            {
                var value = _httpContextAccessor.HttpContext?.User.FindFirstValue(ScopeClaims.LocalUserId);
                return int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out var userId) && userId > 0
                    ? userId
                    : throw new InvalidOperationException("The authenticated token subject must map to a local user.");
            }
        }
    }
}
