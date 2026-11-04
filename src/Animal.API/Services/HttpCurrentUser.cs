using System.Security.Claims;

namespace Animal.API.Services;

public sealed class HttpCurrentUser(IHttpContextAccessor httpContextAccessor) : ICurrentUser
{
    public string Subject =>
        httpContextAccessor.HttpContext?.User.FindFirstValue("sub")
        ?? throw new InvalidOperationException(
            "The authenticated user is missing the required 'sub' claim.");
}
