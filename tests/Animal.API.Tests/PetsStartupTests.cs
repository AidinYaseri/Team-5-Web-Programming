using System.Net.Http.Json;
using Animal.API.Contracts;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Animal.API.Tests
{
    // Uses the real Program.cs registrations (no test overrides) to check the DI wiring.
    public class PetsStartupTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly HttpClient _client;

        public PetsStartupTests(WebApplicationFactory<Program> factory)
        {
            _client = factory.CreateClient();
        }

        // Regression: DI used to pick InMemoryPetService(IEnumerable<Pet>) and pass an empty list, so the API returned [].
        [Fact]
        public async Task GetPets_WithRealServices_ReturnsSeedData()
        {
            var pets = await _client.GetFromJsonAsync<List<PetResponse>>("/api/pets");

            Assert.NotNull(pets);
            Assert.NotEmpty(pets);
        }
    }
}
