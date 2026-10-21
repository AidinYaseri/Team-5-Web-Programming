using Animal.API.Contracts;
using Animal.API.Services;
using Animal.Data.Repositories;
using Animal.Domain.Entities;
using Animal.Domain.Exceptions;

namespace Animal.API.Tests
{
    public class PetServiceTests
    {
        private static PetService CreateService() => new(new InMemoryPetRepository(
        [
            new Pet { Id = 1, Name = "Biscuit", Species = "Dog", Breed = "Beagle", Age = 3, IsAvailable = true, ShelterId = 1 },
            new Pet { Id = 2, Name = "Rex", Species = "Dog", Breed = "German Shepherd", Age = 7, IsAvailable = false, ShelterId = 2 }
        ]));

        // Regression from Milestone 2: GetAvailablePets goes through search and must keep its old behaviour.
        [Fact]
        public async Task GetAvailablePetsAsync_ReturnsOnlyAvailablePets()
        {
            var result = await CreateService().GetAvailablePetsAsync();

            Assert.Equal([1], result.Select(p => p.Id));
        }

        [Fact]
        public async Task GetPetAsync_WhenMissing_ThrowsEntityNotFound()
        {
            var ex = await Assert.ThrowsAsync<EntityNotFoundException>(() => CreateService().GetPetAsync(99));

            Assert.Equal(99, ex.Id);
        }

        [Fact]
        public async Task UpdatePetAsync_WhenMissing_ThrowsEntityNotFound()
        {
            var request = new UpdatePetRequest { Name = "Ghost", Species = "Cat", ShelterId = 1 };

            await Assert.ThrowsAsync<EntityNotFoundException>(() => CreateService().UpdatePetAsync(99, request));
        }

        [Fact]
        public async Task DeletePetAsync_WhenMissing_ThrowsEntityNotFound()
        {
            await Assert.ThrowsAsync<EntityNotFoundException>(() => CreateService().DeletePetAsync(99));
        }

        [Fact]
        public async Task CreatePetAsync_TrimsFieldsAndMarksPetAvailable()
        {
            var request = new CreatePetRequest { Name = "  Nugget ", Species = " Rabbit ", Breed = "Mini Rex", Age = 1, ShelterId = 1 };

            var created = await CreateService().CreatePetAsync(request);

            Assert.Equal(3, created.Id);
            Assert.Equal("Nugget", created.Name);
            Assert.Equal("Rabbit", created.Species);
            Assert.True(created.IsAvailable);
        }
    }
}
