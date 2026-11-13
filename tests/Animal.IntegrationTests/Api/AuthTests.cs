using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using Animal.API.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Animal.IntegrationTests.Api;

public class AuthTests : IClassFixture<AnimalApiFactory>
{
    private readonly AnimalApiFactory _factory;

    public AuthTests(AnimalApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task ProtectedWrite_WithoutAuthorizationHeader_ReturnsUnauthorized()
    {
        using var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/pets", NewPet());

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ProtectedWrite_WithMalformedBearerToken_ReturnsUnauthorized()
    {
        using var client = CreateBearerClient("not-a-jwt");

        var response = await client.PostAsJsonAsync("/api/pets", NewPet());

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ProtectedWrite_WithWrongSigningKey_ReturnsUnauthorized()
    {
        using var wrongKey = RSA.Create(2048);
        using var client = CreateBearerClient(
            _factory.CreateToken(scopes: ["pets.write"], roles: AnimalApiFactory.StaffRoles, signingKey: wrongKey));

        var response = await client.PostAsJsonAsync("/api/pets", NewPet());

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ProtectedWrite_WithExpiredTokenOutsideClockSkew_ReturnsUnauthorized()
    {
        using var client = CreateBearerClient(
            _factory.CreateToken(
                scopes: ["pets.write"], roles: AnimalApiFactory.StaffRoles,
                expiresAt: DateTimeOffset.UtcNow.AddMinutes(-11)));

        var response = await client.PostAsJsonAsync("/api/pets", NewPet());

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ProtectedWrite_WithWrongIssuer_ReturnsUnauthorized()
    {
        using var client = CreateBearerClient(
            _factory.CreateToken(issuer: "https://wrong-issuer.example", scopes: ["pets.write"], roles: AnimalApiFactory.StaffRoles));

        var response = await client.PostAsJsonAsync("/api/pets", NewPet());

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ProtectedWrite_WithWrongAudience_ReturnsUnauthorized()
    {
        using var client = CreateBearerClient(
            _factory.CreateToken(audience: "wrong-audience", scopes: ["pets.write"], roles: AnimalApiFactory.StaffRoles));

        var response = await client.PostAsJsonAsync("/api/pets", NewPet());

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ProtectedWrite_WithValidTokenMissingScope_ReturnsForbidden()
    {
        using var client = CreateBearerClient(
            _factory.CreateToken(scopes: ["users.read"], roles: AnimalApiFactory.StaffRoles));

        var response = await client.PostAsJsonAsync("/api/pets", NewPet());

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task ProtectedWrite_WithPetsWriteAndAnotherScope_ReturnsSuccess()
    {
        using var client = CreateBearerClient(
            _factory.CreateToken(scopes: ["users.read", "pets.write"], roles: AnimalApiFactory.StaffRoles));

        var response = await client.PostAsJsonAsync("/api/pets", NewPet());

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    // Decision D3: the identity provider gives an optional scope to anyone who asks, so staff operations need the role too.
    [Theory]
    [InlineData("pets.write", "POST", "/api/pets")]
    [InlineData("shelters.write", "POST", "/api/shelters")]
    [InlineData("adoptions.review", "GET", "/api/adoptions?status=Pending")]
    public async Task StaffOperation_WithScopeButNoStaffRole_ReturnsForbidden(string scope, string method, string url)
    {
        using var client = CreateBearerClient(_factory.CreateToken(scopes: [scope]));

        var response = await client.SendAsync(StaffRequest(method, url));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Theory]
    [InlineData("pets.write", "POST", "/api/pets")]
    [InlineData("shelters.write", "POST", "/api/shelters")]
    [InlineData("adoptions.review", "GET", "/api/adoptions?status=Pending")]
    public async Task StaffOperation_WithStaffRoleButNoScope_ReturnsForbidden(string scope, string method, string url)
    {
        _ = scope;
        using var client = CreateBearerClient(_factory.CreateToken(scopes: ["users.read"], roles: AnimalApiFactory.StaffRoles));

        var response = await client.SendAsync(StaffRequest(method, url));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Theory]
    [InlineData("pets.write", "POST", "/api/pets", HttpStatusCode.Created)]
    [InlineData("shelters.write", "POST", "/api/shelters", HttpStatusCode.Created)]
    [InlineData("adoptions.review", "GET", "/api/adoptions?status=Pending", HttpStatusCode.OK)]
    public async Task StaffOperation_WithScopeAndStaffRole_IsAllowed(string scope, string method, string url, HttpStatusCode expected)
    {
        using var client = CreateBearerClient(_factory.CreateToken(scopes: [scope], roles: AnimalApiFactory.StaffRoles));

        var response = await client.SendAsync(StaffRequest(method, url));

        Assert.Equal(expected, response.StatusCode);
    }

    [Theory]
    [InlineData("Staff")]
    [InlineData("staff-admin")]
    [InlineData("default-roles-animal")]
    public async Task StaffOperation_WithOtherRole_ReturnsForbidden(string role)
    {
        using var client = CreateBearerClient(_factory.CreateToken(scopes: ["pets.write"], roles: [role]));

        var response = await client.PostAsJsonAsync("/api/pets", NewPet());

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task StaffOperation_RoleOnlyInNestedRealmAccessClaim_ReturnsForbidden()
    {
        // Keycloak's default shape. Only the flat "roles" claim from our mapper counts.
        using var client = CreateBearerClient(_factory.CreateToken(
            scopes: ["pets.write"],
            extraClaims: [new System.Security.Claims.Claim("realm_access", "{\"roles\":[\"staff\"]}", "JSON")]));

        var response = await client.PostAsJsonAsync("/api/pets", NewPet());

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task PublicGet_WithoutAuthorizationHeader_ReturnsSuccess()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/pets");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public void AuthorizationOptions_FallbackPolicy_RequiresAuthenticatedUser()
    {
        var options = _factory.Services
            .GetRequiredService<IOptions<AuthorizationOptions>>()
            .Value;

        Assert.NotNull(options.FallbackPolicy);
        Assert.Contains(
            options.FallbackPolicy.Requirements,
            requirement => requirement is DenyAnonymousAuthorizationRequirement);
    }

    [Fact]
    public void ControllerEndpoints_HaveExplicitAuthorizationMetadata()
    {
        var endpoints = _factory.Services
            .GetRequiredService<EndpointDataSource>()
            .Endpoints
            .Where(endpoint => endpoint.Metadata.GetMetadata<ControllerActionDescriptor>() is not null)
            .ToList();

        Assert.NotEmpty(endpoints);
        Assert.All(
            endpoints,
            endpoint => Assert.True(
                endpoint.Metadata.GetMetadata<IAllowAnonymous>() is not null
                || endpoint.Metadata.GetMetadata<IAuthorizeData>() is not null,
                $"Controller endpoint '{endpoint.DisplayName}' has no authorization metadata."));
    }

    [Fact]
    public async Task OpenApi_WithoutAuthorizationHeader_ReturnsSuccess()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync("/openapi/v1.json");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    private HttpClient CreateBearerClient(string token)
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    private static HttpRequestMessage StaffRequest(string method, string url)
    {
        var request = new HttpRequestMessage(new HttpMethod(method), url);
        if (url.StartsWith("/api/pets", StringComparison.Ordinal))
        {
            request.Content = JsonContent.Create(NewPet());
        }
        else if (url.StartsWith("/api/shelters", StringComparison.Ordinal))
        {
            request.Content = JsonContent.Create(NewShelter());
        }

        return request;
    }

    private static CreateShelterRequest NewShelter() =>
        new()
        {
            Name = "Auth Test Shelter",
            Address = "1 Test Street, Montreal",
            PhoneNumber = "514-555-0100",
            Email = "auth-test-shelter@example.test"
        };

    private static CreatePetRequest NewPet() =>
        new()
        {
            Name = "Auth Test Pet",
            Species = "Rabbit",
            Breed = "Mini Rex",
            Age = 1,
            IsAvailable = true,
            ShelterId = 2
        };
}
