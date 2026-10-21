using Animal.Data.Repositories;
using Animal.Domain.Entities;
using Animal.Domain.ValueObjects;

namespace Animal.Data.Tests
{
    public class InMemoryPetRepositoryTests
    {
        // Fake pets so each test knows exactly what should come back.
        private static List<Pet> TestPets() =>
        [
            new Pet { Id = 1, Name = "Biscuit", Species = "Dog", Breed = "Beagle", Age = 3, IsAvailable = true, ShelterId = 1 },
            new Pet { Id = 2, Name = "Mochi", Species = "Cat", Breed = "Siamese", Age = 1, IsAvailable = true, ShelterId = 1 },
            new Pet { Id = 3, Name = "Rex", Species = "Dog", Breed = "German Shepherd", Age = 7, IsAvailable = false, ShelterId = 2 },
            new Pet { Id = 4, Name = "Pepper", Species = "Rabbit", Breed = "Holland Lop", Age = 2, IsAvailable = true, ShelterId = 2 },
            new Pet { Id = 5, Name = "Luna", Species = "Cat", Breed = "Domestic Shorthair", Age = 9, IsAvailable = true, ShelterId = 1 }
        ];

        private static InMemoryPetRepository CreateRepository() => new(TestPets());

        private static int[] Ids(List<Pet> pets) => pets.Select(p => p.Id).ToArray();

        [Fact]
        public async Task SearchAsync_WithNoFilters_ReturnsAllPetsOrderedById()
        {
            var result = await CreateRepository().SearchAsync(new PetSearchCriteria());

            Assert.Equal([1, 2, 3, 4, 5], Ids(result));
        }

        [Theory]
        [InlineData("Dog")]
        [InlineData("dog")]
        [InlineData("DOG")]
        [InlineData("  dog  ")]
        public async Task SearchAsync_BySpecies_IgnoresCaseAndSurroundingSpaces(string species)
        {
            var result = await CreateRepository().SearchAsync(new PetSearchCriteria { Species = species });

            Assert.Equal([1, 3], Ids(result));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public async Task SearchAsync_WithBlankSpecies_DoesNotFilterBySpecies(string? species)
        {
            var result = await CreateRepository().SearchAsync(new PetSearchCriteria { Species = species });

            Assert.Equal(5, result.Count);
        }

        [Fact]
        public async Task SearchAsync_BySpecies_DoesNotMatchPartialNames()
        {
            var result = await CreateRepository().SearchAsync(new PetSearchCriteria { Species = "Do" });

            Assert.Empty(result);
        }

        [Fact]
        public async Task SearchAsync_ByMaxAge_IncludesPetsExactlyThatAge()
        {
            var result = await CreateRepository().SearchAsync(new PetSearchCriteria { MaxAge = 3 });

            Assert.Equal([1, 2, 4], Ids(result));
        }

        [Fact]
        public async Task SearchAsync_WithMaxAgeZero_ReturnsOnlyNewborns()
        {
            var repository = new InMemoryPetRepository(
            [
                new Pet { Id = 1, Name = "Tiny", Species = "Cat", Breed = "Mixed", Age = 0, IsAvailable = true, ShelterId = 1 },
                new Pet { Id = 2, Name = "Bean", Species = "Cat", Breed = "Mixed", Age = 1, IsAvailable = true, ShelterId = 1 }
            ]);

            var result = await repository.SearchAsync(new PetSearchCriteria { MaxAge = 0 });

            Assert.Equal([1], Ids(result));
        }

        [Fact]
        public async Task SearchAsync_AvailableOnly_SkipsAdoptedPets()
        {
            var result = await CreateRepository().SearchAsync(new PetSearchCriteria { AvailableOnly = true });

            Assert.Equal([1, 2, 4, 5], Ids(result));
        }

        [Fact]
        public async Task SearchAsync_WithAllFilters_CombinesThemWithAnd()
        {
            var criteria = new PetSearchCriteria { Species = "dog", MaxAge = 10, AvailableOnly = true };

            var result = await CreateRepository().SearchAsync(criteria);

            // Rex is a dog under 10 but already adopted, so only Biscuit is left.
            Assert.Equal([1], Ids(result));
        }

        [Fact]
        public async Task SearchAsync_WhenNothingMatches_ReturnsEmptyList()
        {
            var result = await CreateRepository().SearchAsync(new PetSearchCriteria { Species = "Parrot" });

            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public async Task SearchAsync_WithNullCriteria_Throws()
        {
            await Assert.ThrowsAsync<ArgumentNullException>(() => CreateRepository().SearchAsync(null!));
        }

        [Fact]
        public async Task SearchAsync_ReturnsNewList_SoCallersCannotChangeTheStore()
        {
            var repository = CreateRepository();

            (await repository.SearchAsync(new PetSearchCriteria())).Clear();

            Assert.Equal(5, (await repository.GetAllAsync()).Count);
        }

        [Fact]
        public async Task SearchAsync_SeesPetsAddedWithAddAsync()
        {
            var repository = CreateRepository();
            await repository.AddAsync(new Pet { Name = "Nugget", Species = "Rabbit", Breed = "Mini Rex", Age = 1, IsAvailable = true, ShelterId = 1 });

            var result = await repository.SearchAsync(new PetSearchCriteria { Species = "rabbit" });

            Assert.Equal([4, 6], Ids(result));
        }

        [Fact]
        public async Task GetByIdAsync_WhenMissing_ReturnsNull()
        {
            Assert.Null(await CreateRepository().GetByIdAsync(99));
        }

        [Fact]
        public async Task UpdateAsync_And_DeleteAsync_WhenMissing_ReturnFalse()
        {
            var repository = CreateRepository();

            Assert.False(await repository.UpdateAsync(new Pet { Id = 99, Name = "Ghost", Species = "Cat" }));
            Assert.False(await repository.DeleteAsync(99));
        }

        [Fact]
        public async Task DefaultConstructor_LoadsSeedData()
        {
            var repository = new InMemoryPetRepository();

            Assert.NotEmpty(await repository.GetAllAsync());
        }
    }
}
