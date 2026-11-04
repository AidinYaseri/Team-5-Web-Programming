using System.Security.Claims;
using Animal.API.Services;
using Microsoft.AspNetCore.Http;

namespace Animal.UnitTests.Api;

public class CurrentUserTests
{
    [Fact]
    public void Subject_WithSubClaim_ReturnsSubject()
    {
        var context = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(
                [new Claim("sub", "user-123")],
                "test"))
        };
        var currentUser = new HttpCurrentUser(new HttpContextAccessor { HttpContext = context });

        Assert.Equal("user-123", currentUser.Subject);
    }

    [Fact]
    public void Subject_WithoutSubClaim_ThrowsClearException()
    {
        var context = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity([], "test"))
        };
        var currentUser = new HttpCurrentUser(new HttpContextAccessor { HttpContext = context });

        var exception = Assert.Throws<InvalidOperationException>(() => currentUser.Subject);

        Assert.Contains("'sub' claim", exception.Message);
    }
}
