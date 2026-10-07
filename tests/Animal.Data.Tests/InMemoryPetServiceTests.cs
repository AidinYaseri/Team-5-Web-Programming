using Animal.Data.Services;
using Animal.Domain.Models;

namespace Animal.Data.Tests
{
    public class InMemoryPetServiceTests
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

        private static InMemoryPetService CreateService() => new(TestPets());

        private static int[] Ids(List<Pet> pets) => pets.Select(p => p.Id).ToArray();

        [Fact]
        public void SearchPets_WithNoFilters_ReturnsAllPetsOrderedById()
        {
            var result = CreateService().SearchPets(new PetSearchCriteria());

            Assert.Equal([1, 2, 3, 4, 5], Ids(result));
        }

        [Theory]
        [InlineData("Dog")]
        [InlineData("dog")]
        [InlineData("DOG")]
        [InlineData("  dog  ")]
        public void SearchPets_BySpecies_IgnoresCaseAndSurroundingSpaces(string species)
        {
            var result = CreateService().SearchPets(new PetSearchCriteria { Species = species });

            Assert.Equal([1, 3], Ids(result));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void SearchPets_WithBlankSpecies_DoesNotFilterBySpecies(string? species)
        {
            var result = CreateService().SearchPets(new PetSearchCriteria { Species = species });

            Assert.Equal(5, result.Count);
        }

        [Fact]
        public void SearchPets_BySpecies_DoesNotMatchPartialNames()
        {
            var result = CreateService().SearchPets(new PetSearchCriteria { Species = "Do" });

            Assert.Empty(result);
        }

        [Fact]
        public void SearchPets_ByMaxAge_IncludesPetsExactlyThatAge()
        {
            var result = CreateService().SearchPets(new PetSearchCriteria { MaxAge = 3 });

            Assert.Equal([1, 2, 4], Ids(result));
        }

        [Fact]
        public void SearchPets_WithMaxAgeZero_ReturnsOnlyNewborns()
        {
            var service = new InMemoryPetService(
            [
                new Pet { Id = 1, Name = "Tiny", Species = "Cat", Breed = "Mixed", Age = 0, IsAvailable = true, ShelterId = 1 },
                new Pet { Id = 2, Name = "Bean", Species = "Cat", Breed = "Mixed", Age = 1, IsAvailable = true, ShelterId = 1 }
            ]);

            var result = service.SearchPets(new PetSearchCriteria { MaxAge = 0 });

            Assert.Equal([1], Ids(result));
        }

        [Fact]
        public void SearchPets_AvailableOnly_SkipsAdoptedPets()
        {
            var result = CreateService().SearchPets(new PetSearchCriteria { AvailableOnly = true });

            Assert.Equal([1, 2, 4, 5], Ids(result));
        }

        [Fact]
        public void SearchPets_WithAllFilters_CombinesThemWithAnd()
        {
            var criteria = new PetSearchCriteria { Species = "dog", MaxAge = 10, AvailableOnly = true };

            var result = CreateService().SearchPets(criteria);

            // Rex is a dog under 10 but already adopted, so only Biscuit is left.
            Assert.Equal([1], Ids(result));
        }

        [Fact]
        public void SearchPets_WhenNothingMatches_ReturnsEmptyList()
        {
            var result = CreateService().SearchPets(new PetSearchCriteria { Species = "Parrot" });

            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public void SearchPets_WithNullCriteria_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => CreateService().SearchPets(null!));
        }

        [Fact]
        public void SearchPets_ReturnsNewList_SoCallersCannotChangeTheStore()
        {
            var service = CreateService();

            service.SearchPets(new PetSearchCriteria()).Clear();

            Assert.Equal(5, service.GetPets().Count);
        }

        [Fact]
        public void SearchPets_SeesPetsAddedWithCreatePet()
        {
            var service = CreateService();
            service.CreatePet(new Pet { Name = "Nugget", Species = "Rabbit", Breed = "Mini Rex", Age = 1, IsAvailable = true, ShelterId = 1 });

            var result = service.SearchPets(new PetSearchCriteria { Species = "rabbit" });

            Assert.Equal([4, 6], Ids(result));
        }

        // Regression: GetAvailablePets now goes through SearchPets and must keep its old behaviour.
        [Fact]
        public void GetAvailablePets_StillReturnsOnlyAvailablePets()
        {
            var result = CreateService().GetAvailablePets();

            Assert.Equal([1, 2, 4, 5], Ids(result));
            Assert.All(result, p => Assert.True(p.IsAvailable));
        }

        [Fact]
        public void DefaultConstructor_LoadsSeedData()
        {
            var service = new InMemoryPetService();

            Assert.NotEmpty(service.GetPets());
        }
    }
}
