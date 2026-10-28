using System.Net.Http.Json;
using Animal.API.Contracts;

namespace Animal.API.Tests
{
    // Uses the real Program.cs registrations (only the database provider is swapped) to check the DI wiring.
    public class PetsStartupTests : IClassFixture<AnimalApiFactory>
    {
        private readonly HttpClient _client;

        public PetsStartupTests(AnimalApiFactory factory)
        {
            _client = factory.CreateClient();
        }

        // Controller -> PetService -> PetRepository -> AnimalDbContext all resolve, and the seed data comes back.
        [Fact]
        public async Task GetPets_WithRealServices_ReturnsSeedData()
        {
            var pets = await _client.GetFromJsonAsync<List<PetResponse>>("/api/pets");

            Assert.NotNull(pets);
            Assert.Equal(5, pets.Count);
        }
    }
}
