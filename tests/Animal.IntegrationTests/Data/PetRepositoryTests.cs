using Animal.Data.Repositories;
using Animal.Domain.Entities;
using Animal.Domain.Queries;
using Microsoft.EntityFrameworkCore;

namespace Animal.IntegrationTests.Data
{
    // Runs against the seed data: 1 Biscuit (Dog, 3), 2 Mochi (Cat, 1), 3 Rex (Dog, 7, adopted), 4 Pepper (Rabbit, 2), 5 Luna (Cat, 9).
    public class PetRepositoryTests : IDisposable
    {
        private readonly SqliteTestDatabase _database = new();

        public void Dispose() => _database.Dispose();

        private static int[] Ids(List<Pet> pets) => pets.Select(p => p.Id).ToArray();

        [Fact]
        public async Task GetAllAsync_ReturnsSeededPetsOrderedById()
        {
            using var context = _database.CreateContext();

            var pets = await new PetRepository(context).GetAllAsync();

            Assert.Equal([1, 2, 3, 4, 5], Ids(pets));
        }

        [Fact]
        public async Task GetByIdAsync_ExistingPet_ReturnsIt()
        {
            using var context = _database.CreateContext();

            var pet = await new PetRepository(context).GetByIdAsync(2);

            Assert.NotNull(pet);
            Assert.Equal("Mochi", pet.Name);
        }

        [Fact]
        public async Task GetByIdAsync_MissingPet_ReturnsNull()
        {
            using var context = _database.CreateContext();

            Assert.Null(await new PetRepository(context).GetByIdAsync(999));
        }

        [Theory]
        [InlineData("Dog")]
        [InlineData("dog")]
        [InlineData("DOG")]
        [InlineData("  dog  ")]
        public async Task SearchAsync_BySpecies_IgnoresCaseAndSurroundingSpaces(string species)
        {
            using var context = _database.CreateContext();

            var pets = await new PetRepository(context).SearchAsync(new PetSearchCriteria { Species = species });

            Assert.Equal([1, 3], Ids(pets));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public async Task SearchAsync_BlankSpecies_DoesNotFilter(string? species)
        {
            using var context = _database.CreateContext();

            var pets = await new PetRepository(context).SearchAsync(new PetSearchCriteria { Species = species });

            Assert.Equal(5, pets.Count);
        }

        [Fact]
        public async Task SearchAsync_BySpecies_DoesNotMatchPartialNames()
        {
            using var context = _database.CreateContext();

            Assert.Empty(await new PetRepository(context).SearchAsync(new PetSearchCriteria { Species = "Do" }));
        }

        [Fact]
        public async Task SearchAsync_ByMaxAge_IncludesPetsExactlyThatAge()
        {
            using var context = _database.CreateContext();

            var pets = await new PetRepository(context).SearchAsync(new PetSearchCriteria { MaxAge = 3 });

            Assert.Equal([1, 2, 4], Ids(pets));
        }

        [Fact]
        public async Task SearchAsync_AvailableOnly_SkipsAdoptedPets()
        {
            using var context = _database.CreateContext();

            var pets = await new PetRepository(context).SearchAsync(new PetSearchCriteria { AvailableOnly = true });

            Assert.Equal([1, 2, 4, 5], Ids(pets));
        }

        [Fact]
        public async Task SearchAsync_WithAllFilters_CombinesThemWithAnd()
        {
            using var context = _database.CreateContext();

            var criteria = new PetSearchCriteria { Species = "dog", MaxAge = 10, AvailableOnly = true };
            var pets = await new PetRepository(context).SearchAsync(criteria);

            Assert.Equal([1], Ids(pets));
        }

        [Fact]
        public async Task SearchAsync_NullCriteria_Throws()
        {
            using var context = _database.CreateContext();

            await Assert.ThrowsAsync<ArgumentNullException>(() => new PetRepository(context).SearchAsync(null!));
        }

        [Fact]
        public async Task Add_IsOnlySavedAfterSaveChanges()
        {
            using (var context = _database.CreateContext())
            {
                new PetRepository(context).Add(new Pet { Name = "Nugget", Species = "Rabbit", Breed = "Mini Rex", Age = 1, IsAvailable = true, ShelterId = 1 });

                using (var other = _database.CreateContext())
                {
                    Assert.Equal(5, await other.Pets.CountAsync());
                }

                await context.SaveChangesAsync();
            }

            using var check = _database.CreateContext();
            var pets = await new PetRepository(check).SearchAsync(new PetSearchCriteria { Species = "rabbit" });

            Assert.Equal([4, 6], Ids(pets));
        }

        [Fact]
        public async Task GetByIdAsync_ReturnsTrackedPet_SoChangesCanBeSaved()
        {
            using (var context = _database.CreateContext())
            {
                var pet = await new PetRepository(context).GetByIdAsync(1);
                pet!.IsAvailable = false;
                await context.SaveChangesAsync();
            }

            using var check = _database.CreateContext();
            Assert.False((await check.Pets.SingleAsync(p => p.Id == 1)).IsAvailable);
        }

        [Fact]
        public async Task Remove_DeletesThePet()
        {
            using (var context = _database.CreateContext())
            {
                var repository = new PetRepository(context);
                repository.Remove((await repository.GetByIdAsync(4))!);
                await context.SaveChangesAsync();
            }

            using var check = _database.CreateContext();
            Assert.Null(await new PetRepository(check).GetByIdAsync(4));
        }

        // Checks the Restrict rule in PetConfiguration: a shelter that still has pets can't be deleted.
        [Fact]
        public async Task DeletingShelterWithPets_IsRejected()
        {
            using var context = _database.CreateContext();
            context.Shelters.Remove(await context.Shelters.SingleAsync(s => s.Id == 1));

            await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
        }

        [Fact]
        public async Task AddingPetWithUnknownShelter_IsRejected()
        {
            using var context = _database.CreateContext();
            new PetRepository(context).Add(new Pet { Name = "Ghost", Species = "Cat", Breed = "Mixed", Age = 2, IsAvailable = true, ShelterId = 99 });

            await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
        }
    }
}
