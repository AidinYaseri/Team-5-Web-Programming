using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using Animal.API.Contracts;

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
            _factory.CreateToken(scopes: ["pets.write"], signingKey: wrongKey));

        var response = await client.PostAsJsonAsync("/api/pets", NewPet());

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ProtectedWrite_WithExpiredTokenOutsideClockSkew_ReturnsUnauthorized()
    {
        using var client = CreateBearerClient(
            _factory.CreateToken(
                scopes: ["pets.write"],
                expiresAt: DateTimeOffset.UtcNow.AddMinutes(-11)));

        var response = await client.PostAsJsonAsync("/api/pets", NewPet());

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ProtectedWrite_WithWrongIssuer_ReturnsUnauthorized()
    {
        using var client = CreateBearerClient(
            _factory.CreateToken(issuer: "https://wrong-issuer.example", scopes: ["pets.write"]));

        var response = await client.PostAsJsonAsync("/api/pets", NewPet());

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ProtectedWrite_WithWrongAudience_ReturnsUnauthorized()
    {
        using var client = CreateBearerClient(
            _factory.CreateToken(audience: "wrong-audience", scopes: ["pets.write"]));

        var response = await client.PostAsJsonAsync("/api/pets", NewPet());

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ProtectedWrite_WithValidTokenMissingScope_ReturnsForbidden()
    {
        using var client = CreateBearerClient(
            _factory.CreateToken(scopes: ["users.read"]));

        var response = await client.PostAsJsonAsync("/api/pets", NewPet());

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task ProtectedWrite_WithPetsWriteAndAnotherScope_ReturnsSuccess()
    {
        using var client = CreateBearerClient(
            _factory.CreateToken(scopes: ["users.read", "pets.write"]));

        var response = await client.PostAsJsonAsync("/api/pets", NewPet());

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task PublicGet_WithoutAuthorizationHeader_ReturnsSuccess()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/pets");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    private HttpClient CreateBearerClient(string token)
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

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
