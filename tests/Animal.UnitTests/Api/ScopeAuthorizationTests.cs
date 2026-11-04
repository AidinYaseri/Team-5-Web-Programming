using System.Security.Claims;
using Animal.API.Authorization;

namespace Animal.UnitTests.Api;

public class ScopeAuthorizationTests
{
    [Fact]
    public void HasScope_ScopeClaimWithMultipleValues_ReturnsTrueForRequiredScope()
    {
        var user = UserWithClaim("scope", "users.read pets.write");

        Assert.True(ScopeClaims.HasScope(user, "pets.write"));
    }

    [Fact]
    public void HasScope_ScpClaim_ReturnsTrueForRequiredScope()
    {
        var user = UserWithClaim("scp", "pets.write");

        Assert.True(ScopeClaims.HasScope(user, "pets.write"));
    }

    [Fact]
    public void HasScope_MissingClaim_ReturnsFalse()
    {
        var user = new ClaimsPrincipal(new ClaimsIdentity());

        Assert.False(ScopeClaims.HasScope(user, "pets.write"));
    }

    [Fact]
    public void HasScope_DifferentScope_ReturnsFalse()
    {
        var user = UserWithClaim("scope", "users.read");

        Assert.False(ScopeClaims.HasScope(user, "pets.write"));
    }

    private static ClaimsPrincipal UserWithClaim(string type, string value) =>
        new(new ClaimsIdentity([new Claim(type, value)], "test"));
}
