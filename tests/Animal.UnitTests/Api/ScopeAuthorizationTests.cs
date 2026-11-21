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

    [Fact]
    public void HasScope_ScopeWithExtraSegment_ReturnsFalse()
    {
        var user = UserWithClaim("scope", "pets.write.all");

        Assert.False(ScopeClaims.HasScope(user, "pets.write"));
    }

    [Fact]
    public void HasScope_DifferentCase_ReturnsFalse()
    {
        var user = UserWithClaim("scope", "PETS.WRITE");

        Assert.False(ScopeClaims.HasScope(user, "pets.write"));
    }

    [Fact]
    public void HasRole_OneOfSeveralRoleClaims_ReturnsTrue()
    {
        var user = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim("roles", "offline_access"), new Claim("roles", "staff")], "test"));

        Assert.True(ScopeClaims.HasRole(user, ScopeClaims.StaffRole));
    }

    [Theory]
    [InlineData("roles", "Staff")]
    [InlineData("roles", "staff-admin")]
    [InlineData("role", "staff")]
    [InlineData("scope", "staff")]
    public void HasRole_WrongCaseNameOrClaimType_ReturnsFalse(string type, string value)
    {
        var user = UserWithClaim(type, value);

        Assert.False(ScopeClaims.HasRole(user, ScopeClaims.StaffRole));
    }

    [Fact]
    public void HasRole_NoClaims_ReturnsFalse()
    {
        var user = new ClaimsPrincipal(new ClaimsIdentity());

        Assert.False(ScopeClaims.HasRole(user, ScopeClaims.StaffRole));
    }

    private static ClaimsPrincipal UserWithClaim(string type, string value) =>
        new(new ClaimsIdentity([new Claim(type, value)], "test"));
}
