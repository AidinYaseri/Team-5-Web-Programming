using System.Net;
using System.Net.Http.Json;
using Animal.API.Contracts;
using Animal.Data.Context;
using Animal.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace Animal.IntegrationTests.Api
{
    public class PetsEndpointTests : IClassFixture<AnimalApiFactory>
    {
        private readonly AnimalApiFactory _factory;
        private readonly HttpClient _client;

        public PetsEndpointTests(AnimalApiFactory factory)
        {
            _factory = factory;

            // xUnit makes a new instance per test, so every test starts from these known pets, not the seed data.
            factory.ResetPets(
            [
                new Pet { Id = 1, Name = "Biscuit", Species = "Dog", Breed = "Beagle", Age = 3, IsAvailable = true, ShelterId = 1 },
                new Pet { Id = 2, Name = "Mochi", Species = "Cat", Breed = "Siamese", Age = 1, IsAvailable = true, ShelterId = 1 },
                new Pet { Id = 3, Name = "Rex", Species = "Dog", Breed = "German Shepherd", Age = 7, IsAvailable = false, ShelterId = 2 }
            ]);

            _client = factory.CreateClient();
        }

        private static CreatePetRequest NewPet() =>
            new() { Name = "Nugget", Species = "Rabbit", Breed = "Mini Rex", Age = 1, IsAvailable = true, ShelterId = 2 };

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


        [Fact]
        public async Task GetPet_Existing_ReturnsPet()
        {
            var pet = await _client.GetFromJsonAsync<PetResponse>("/api/pets/2");

            Assert.NotNull(pet);
            Assert.Equal("Mochi", pet.Name);
        }

        [Fact]
        public async Task GetPet_Missing_ReturnsNotFoundProblem()
        {
            var response = await _client.GetAsync("/api/pets/999");

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
            Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        }

        [Fact]
        public async Task CreatePet_Valid_ReturnsCreatedWithLocation()
        {
            var response = await _client.PostAsJsonAsync("/api/pets", NewPet());

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            var created = await response.Content.ReadFromJsonAsync<PetResponse>();
            Assert.NotNull(created);
            Assert.Equal("Nugget", created.Name);
            Assert.EndsWith($"/api/pets/{created.Id}", response.Headers.Location?.ToString(), StringComparison.OrdinalIgnoreCase);

            // It really was saved: a separate request finds it.
            var fetched = await _client.GetFromJsonAsync<PetResponse>($"/api/pets/{created.Id}");
            Assert.Equal(created, fetched);
        }

        [Fact]
        public async Task CreatePet_MissingName_ReturnsValidationProblem()
        {
            var request = NewPet();
            request.Name = "";

            var response = await _client.PostAsJsonAsync("/api/pets", request);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task CreatePet_UnknownShelter_ReturnsBadRequestFromDomainCheck()
        {
            var request = NewPet();
            request.ShelterId = 99;

            var response = await _client.PostAsJsonAsync("/api/pets", request);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Contains("Shelter 99 does not exist.", await response.Content.ReadAsStringAsync());
        }

        [Fact]
        public async Task UpdatePet_Existing_ReturnsNoContentAndSaves()
        {
            var request = new UpdatePetRequest { Name = "Biscuit", Species = "Dog", Breed = "Beagle", Age = 4, IsAvailable = false, ShelterId = 1 };

            var response = await _client.PutAsJsonAsync("/api/pets/1", request);

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            var pet = await _client.GetFromJsonAsync<PetResponse>("/api/pets/1");
            Assert.Equal(4, pet!.Age);
            Assert.False(pet.IsAvailable);
        }

        [Fact]
        public async Task UpdatePet_Missing_ReturnsNotFound()
        {
            var request = new UpdatePetRequest { Name = "Ghost", Species = "Cat", Age = 2, ShelterId = 1 };

            var response = await _client.PutAsJsonAsync("/api/pets/999", request);

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task DeletePet_Existing_ReturnsNoContentAndRemovesIt()
        {
            var response = await _client.DeleteAsync("/api/pets/3");

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, (await _client.GetAsync("/api/pets/3")).StatusCode);
        }

        [Fact]
        public async Task DeletePet_Missing_ReturnsNotFound()
        {
            var response = await _client.DeleteAsync("/api/pets/999");

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        // The Restrict foreign key blocks the delete, and DomainExceptionHandler turns the DbUpdateException into a 409.
        [Fact]
        public async Task DeletePet_WithAdoptionApplication_ReturnsConflict()
        {
            using (var scope = _factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<AnimalDbContext>();
                context.Users.Add(new User { Id = 1, Name = "Test User", Email = "test.user@example.com", PhoneNumber = "555-0199" });
                context.AdoptionApplications.Add(new AdoptionApplication { UserId = 1, PetId = 1, ApplicationDate = DateTime.UtcNow });
                context.SaveChanges();
            }

            try
            {
                var response = await _client.DeleteAsync("/api/pets/1");

                Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
                Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
            }
            finally
            {
                // Clean up so ResetPets can delete pet 1 for the next test.
                using var scope = _factory.Services.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<AnimalDbContext>();
                context.AdoptionApplications.RemoveRange(context.AdoptionApplications);
                context.Users.RemoveRange(context.Users);
                context.SaveChanges();
            }
        }
    }
}
