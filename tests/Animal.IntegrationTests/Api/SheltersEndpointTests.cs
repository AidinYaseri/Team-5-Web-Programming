using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Animal.API.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace Animal.IntegrationTests.Api
{
    public class SheltersEndpointTests : IClassFixture<AnimalApiFactory>
    {
        private readonly AnimalApiFactory _factory;
        private readonly HttpClient _client;

        public SheltersEndpointTests(AnimalApiFactory factory)
        {
            _factory = factory;
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task GetShelters_ReturnsSeededShelters()
        {
            var shelters = await _client.GetFromJsonAsync<List<ShelterResponse>>("/api/shelters");

            Assert.NotNull(shelters);
            Assert.True(shelters.Count >= 2);
        }

        [Fact]
        public async Task GetShelterPets_ExistingShelter_ReturnsShelterPets()
        {
            var pets = await _client.GetFromJsonAsync<List<PetResponse>>("/api/shelters/1/pets");

            Assert.NotNull(pets);
            Assert.NotEmpty(pets);
            Assert.All(pets, pet => Assert.Equal(1, pet.ShelterId));
        }

        [Fact]
        public async Task GetShelterPets_MissingShelter_ReturnsNotFound()
        {
            var response = await _client.GetAsync("/api/shelters/99999/pets");

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task CreateShelter_WithoutAuthentication_Returns401()
        {
            var request = ValidShelter();

            var response = await _client.PostAsJsonAsync("/api/shelters", request);

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task CreateShelter_WithoutWritePermission_Returns403()
        {
            var request = ValidShelter();

            using var message = new HttpRequestMessage(HttpMethod.Post, "/api/shelters")
            {
                Content = JsonContent.Create(request)
            };
            message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _factory.CreateToken(scopes: ["pets.read"]));

            using var response = await _client.SendAsync(message);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task CreateShelter_WithWritePermission_ReturnsCreated()
        {
            var request = ValidShelter();

            using var message = new HttpRequestMessage(HttpMethod.Post, "/api/shelters")
            {
                Content = JsonContent.Create(request)
            };
            message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _factory.CreateToken(scopes: ["shelters.write"], roles: AnimalApiFactory.StaffRoles));

            using var response = await _client.SendAsync(message);

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);

            var created = await response.Content.ReadFromJsonAsync<ShelterResponse>();
            Assert.NotNull(created);
            Assert.EndsWith($"/api/shelters/{created.Id}", response.Headers.Location?.ToString(), StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task UpdateShelter_MissingShelter_Returns404()
        {
            using var message = new HttpRequestMessage(HttpMethod.Put, "/api/shelters/99999")
            {
                Content = JsonContent.Create(ValidShelter())
            };
            message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _factory.CreateToken(scopes: ["shelters.write"], roles: AnimalApiFactory.StaffRoles));

            using var response = await _client.SendAsync(message);

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task DeleteShelter_WithPets_Returns409()
        {
            using var message = new HttpRequestMessage(HttpMethod.Delete, "/api/shelters/1");
            message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _factory.CreateToken(scopes: ["shelters.write"], roles: AnimalApiFactory.StaffRoles));

            using var response = await _client.SendAsync(message);

            Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
            var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
            Assert.NotNull(problem);
        }

        private static CreateShelterRequest ValidShelter() => new()
        {
            Name = "Northside Rescue",
            Address = "88 Oak Avenue",
            PhoneNumber = "+1 (555) 0199",
            Email = "northside@example.org"
        };
    }
}