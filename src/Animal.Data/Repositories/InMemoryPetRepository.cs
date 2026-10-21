using Animal.Data.SeedData;
using Animal.Domain.Entities;
using Animal.Domain.Services;
using Animal.Domain.ValueObjects;

namespace Animal.Data.Repositories
{
    // Temporary IPetRepository backed by a list. Replaced by an EF Core repository in WI-2.
    public class InMemoryPetRepository : IPetRepository
    {
        private readonly List<Pet> _pets;
        private readonly object _lock = new();

        public InMemoryPetRepository() : this(PetSeedData.GetPets())
        {
        }

        public InMemoryPetRepository(IEnumerable<Pet> pets)
        {
            _pets = pets.ToList();
        }

        public Task<Pet?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            lock (_lock)
            {
                return Task.FromResult(_pets.FirstOrDefault(p => p.Id == id));
            }
        }

        public Task<List<Pet>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            lock (_lock)
            {
                return Task.FromResult(_pets.OrderBy(p => p.Id).ToList());
            }
        }

        public Task<List<Pet>> SearchAsync(PetSearchCriteria criteria, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(criteria);

            lock (_lock)
            {
                return Task.FromResult(_pets
                    .Where(p => PetRules.Matches(p, criteria))
                    .OrderBy(p => p.Id)
                    .ToList());
            }
        }

        public Task<Pet> AddAsync(Pet pet, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(pet);

            lock (_lock)
            {
                pet.Id = _pets.Count == 0 ? 1 : _pets.Max(p => p.Id) + 1;
                _pets.Add(pet);
                return Task.FromResult(pet);
            }
        }

        public Task<bool> UpdateAsync(Pet pet, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(pet);

            lock (_lock)
            {
                var index = _pets.FindIndex(p => p.Id == pet.Id);
                if (index < 0)
                {
                    return Task.FromResult(false);
                }

                _pets[index] = pet;
                return Task.FromResult(true);
            }
        }

        public Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
        {
            lock (_lock)
            {
                return Task.FromResult(_pets.RemoveAll(p => p.Id == id) > 0);
            }
        }
    }
}
