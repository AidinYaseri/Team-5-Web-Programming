using System.Net;
using System.Net.Http.Json;
using Animal.API.Contracts;
using Animal.Data.Services;
using Animal.Domain.Models;
using Animal.Domain.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace Animal.API.Tests
{
    public class PetsEndpointTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly HttpClient _client;

        public PetsEndpointTests(WebApplicationFactory<Program> factory)
        {
            // Swap the real IPetService for one with known fake pets so the results don't depend on seed data.
            _client = factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureTestServices(services =>
                {
                    services.AddSingleton<IPetService>(new InMemoryPetService(
                    [
                        new Pet { Id = 1, Name = "Biscuit", Species = "Dog", Breed = "Beagle", Age = 3, IsAvailable = true, ShelterId = 1 },
                        new Pet { Id = 2, Name = "Mochi", Species = "Cat", Breed = "Siamese", Age = 1, IsAvailable = true, ShelterId = 1 },
                        new Pet { Id = 3, Name = "Rex", Species = "Dog", Breed = "German Shepherd", Age = 7, IsAvailable = false, ShelterId = 2 }
                    ]));
                });
            }).CreateClient();
        }

        private async Task<int[]> GetIdsAsync(string url)
        {
            var response = await _client.GetAsync(url);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var pets = await response.Content.ReadFromJsonAsync<List<PetResponse>>();
            Assert.NotNull(pets);
            return pets.Select(p => p.Id).ToArray();
        }

        [Fact]
        public async Task GetPets_WithoutQuery_ReturnsAllPets()
        {
            var ids = await GetIdsAsync("/api/pets");

            Assert.Equal([1, 2, 3], ids);
        }

        [Fact]
        public async Task GetPets_BySpecies_ReturnsMatchingPets()
        {
            var ids = await GetIdsAsync("/api/pets?species=dog");

            Assert.Equal([1, 3], ids);
        }

        [Fact]
        public async Task GetPets_BySpecies_UppercaseIsEquivalent()
        {
            var lower = await GetIdsAsync("/api/pets?species=dog");
            var upper = await GetIdsAsync("/api/pets?species=DOG");

            Assert.Equal(lower, upper);
        }

        [Fact]
        public async Task GetPets_WithAllFilters_ReturnsOnlyPetsMatchingEveryFilter()
        {
            var ids = await GetIdsAsync("/api/pets?species=Dog&maxAge=5&availableOnly=true");

            Assert.Equal([1], ids);
        }

        [Fact]
        public async Task GetPets_SpeciesExactlyFiftyCharacters_ReturnsOk()
        {
            var fifty = new string('a', 50);
            var ids = await GetIdsAsync($"/api/pets?species={fifty}");

            // GetIdsAsync already checks for 200, so this proves the length is allowed. No pet has that species.
            Assert.Empty(ids);
        }

        [Fact]
        public async Task GetPets_WhenNothingMatches_ReturnsEmptyArray()
        {
            Assert.Empty(await GetIdsAsync("/api/pets?species=parrot"));
        }

        [Fact]
        public async Task GetPets_MaxAge100_ReturnsAllPets()
        {
            var ids = await GetIdsAsync("/api/pets?maxAge=100");

            Assert.Equal([1, 2, 3], ids);
        }

        [Fact]
        public async Task GetPets_ReturnsDtoFieldsAsCamelCaseJson()
        {
            var json = await _client.GetStringAsync("/api/pets?species=cat");

            Assert.Contains("\"name\":\"Mochi\"", json);
            Assert.Contains("\"isAvailable\":true", json);
        }

        [Theory]
        [InlineData("/api/pets?maxAge=-1")]
        [InlineData("/api/pets?maxAge=101")]
        [InlineData("/api/pets?maxAge=abc")]
        [InlineData("/api/pets?maxAge=3.5")]
        [InlineData("/api/pets?availableOnly=maybe")]
        public async Task GetPets_WithInvalidQuery_ReturnsValidationProblem(string url)
        {
            var response = await _client.GetAsync(url);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
            var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
            Assert.NotNull(problem);
            Assert.NotEmpty(problem.Errors);
        }

        [Fact]
        public async Task GetPets_WithSpeciesLongerThan50Characters_ReturnsBadRequest()
        {
            var response = await _client.GetAsync("/api/pets?species=" + new string('a', 51));

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task GetPets_AvailableOnlyFalse_EqualsNoFilter()
        {
            var none = await GetIdsAsync("/api/pets");
            var withFalse = await GetIdsAsync("/api/pets?availableOnly=false");

            Assert.Equal(none, withFalse);
        }
    }
}
