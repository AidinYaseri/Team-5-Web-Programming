using Animal.API.Contracts;
using Animal.API.Services;
using Animal.Data.Repositories;
using Animal.Domain.Entities;
using Animal.Domain.Exceptions;
using Animal.Domain.Queries;

namespace Animal.UnitTests.Api
{
    // Tests the orchestration in PetService with fake repositories, so no database is involved.
    public class PetServiceTests
    {
        private readonly FakePetRepository _pets = new();
        private readonly FakeShelterRepository _shelters = new(1, 2);
        private readonly FakeUnitOfWork _unitOfWork = new();
        private readonly PetService _service;

        public PetServiceTests()
        {
            _pets.Items.Add(new Pet { Id = 1, Name = "Biscuit", Species = "Dog", Breed = "Beagle", Age = 3, IsAvailable = true, ShelterId = 1 });
            _service = new PetService(_pets, _shelters, _unitOfWork);
        }

        private static CreatePetRequest NewPet() =>
            new() { Name = "Nugget", Species = "Rabbit", Breed = "Mini Rex", Age = 1, IsAvailable = true, ShelterId = 2 };

        [Fact]
        public async Task SearchPetsAsync_PassesTheFiltersToTheRepository()
        {
            await _service.SearchPetsAsync(new PetSearchQuery { Species = "dog", MaxAge = 5, AvailableOnly = true });

            Assert.NotNull(_pets.LastCriteria);
            Assert.Equal("dog", _pets.LastCriteria.Species);
            Assert.Equal(5, _pets.LastCriteria.MaxAge);
            Assert.True(_pets.LastCriteria.AvailableOnly);
        }

        [Fact]
        public async Task SearchPetsAsync_ReturnsDtos()
        {
            var result = await _service.SearchPetsAsync(new PetSearchQuery());

            Assert.Equal([new PetResponse(1, "Biscuit", "Dog", "Beagle", 3, true, 1)], result);
        }

        [Fact]
        public async Task GetPetAsync_Missing_ThrowsNotFound()
        {
            await Assert.ThrowsAsync<EntityNotFoundException>(() => _service.GetPetAsync(999));
        }

        [Fact]
        public async Task CreatePetAsync_Valid_AddsTrimsAndSavesOnce()
        {
            var request = NewPet();
            request.Name = "  Nugget  ";

            var result = await _service.CreatePetAsync(request);

            Assert.Equal("Nugget", result.Name);
            Assert.Equal(2, _pets.Items.Count);
            Assert.Equal(1, _unitOfWork.SaveCount);
        }

        [Fact]
        public async Task CreatePetAsync_MissingBreed_StoresEmptyString()
        {
            var request = NewPet();
            request.Breed = null;

            var result = await _service.CreatePetAsync(request);

            Assert.Equal("", result.Breed);
        }

        // The DTO attributes normally catch this first. The domain rule is the safety net if something skips them.
        [Fact]
        public async Task CreatePetAsync_BreaksDomainRule_ThrowsAndSavesNothing()
        {
            var request = NewPet();
            request.Age = -1;

            await Assert.ThrowsAsync<DomainValidationException>(() => _service.CreatePetAsync(request));
            Assert.Single(_pets.Items);
            Assert.Equal(0, _unitOfWork.SaveCount);
        }

        [Fact]
        public async Task CreatePetAsync_UnknownShelter_ThrowsAndSavesNothing()
        {
            var request = NewPet();
            request.ShelterId = 99;

            var ex = await Assert.ThrowsAsync<DomainValidationException>(() => _service.CreatePetAsync(request));
            Assert.Contains("Shelter 99 does not exist.", ex.Errors);
            Assert.Equal(0, _unitOfWork.SaveCount);
        }

        [Fact]
        public async Task UpdatePetAsync_Existing_ChangesFieldsAndSavesOnce()
        {
            var request = new UpdatePetRequest { Name = "Biscuit", Species = "Dog", Breed = "Beagle", Age = 4, IsAvailable = false, ShelterId = 2 };

            await _service.UpdatePetAsync(1, request);

            var pet = _pets.Items.Single();
            Assert.Equal(4, pet.Age);
            Assert.False(pet.IsAvailable);
            Assert.Equal(2, pet.ShelterId);
            Assert.Equal(1, _unitOfWork.SaveCount);
        }

        [Fact]
        public async Task UpdatePetAsync_Missing_ThrowsNotFound()
        {
            var request = new UpdatePetRequest { Name = "Ghost", Species = "Cat", Age = 2, ShelterId = 1 };

            await Assert.ThrowsAsync<EntityNotFoundException>(() => _service.UpdatePetAsync(999, request));
            Assert.Equal(0, _unitOfWork.SaveCount);
        }

        [Fact]
        public async Task DeletePetAsync_Existing_RemovesAndSavesOnce()
        {
            await _service.DeletePetAsync(1);

            Assert.Empty(_pets.Items);
            Assert.Equal(1, _unitOfWork.SaveCount);
        }

        [Fact]
        public async Task DeletePetAsync_Missing_ThrowsNotFound()
        {
            await Assert.ThrowsAsync<EntityNotFoundException>(() => _service.DeletePetAsync(999));
            Assert.Equal(0, _unitOfWork.SaveCount);
        }

        private sealed class FakePetRepository : IPetRepository
        {
            public List<Pet> Items { get; } = [];
            public PetSearchCriteria? LastCriteria { get; private set; }

            public Task<Pet?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
                Task.FromResult(Items.FirstOrDefault(p => p.Id == id));

            public Task<List<Pet>> GetAllAsync(CancellationToken cancellationToken = default) =>
                Task.FromResult(Items.ToList());

            // Filtering is tested in PetRepositoryTests. Here we only record what the service asked for.
            public Task<List<Pet>> SearchAsync(PetSearchCriteria criteria, CancellationToken cancellationToken = default)
            {
                LastCriteria = criteria;
                return Task.FromResult(Items.ToList());
            }

            public void Add(Pet pet)
            {
                pet.Id = Items.Count == 0 ? 1 : Items.Max(p => p.Id) + 1;
                Items.Add(pet);
            }

            public void Remove(Pet pet) => Items.Remove(pet);
        }

        private sealed class FakeShelterRepository(params int[] ids) : IShelterRepository
        {
            public Task<bool> ExistsAsync(int id, CancellationToken cancellationToken = default) =>
                Task.FromResult(ids.Contains(id));
        }

        private sealed class FakeUnitOfWork : IUnitOfWork
        {
            public int SaveCount { get; private set; }

            public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
            {
                SaveCount++;
                return Task.FromResult(1);
            }
        }
    }
}
