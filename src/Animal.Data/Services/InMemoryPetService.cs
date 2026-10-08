using Animal.Data.SeedData;
using Animal.Domain.Models;
using Animal.Domain.Services;

namespace Animal.Data.Services
{
    // Temporary IPetService backed by a list. Swap for an EF Core implementation once AnimalDbContext exists.
    public class InMemoryPetService : IPetService
    {
        private readonly List<Pet> _pets;
        private readonly object _lock = new();

        public InMemoryPetService() : this(PetSeedData.GetPets())
        {
        }

        public InMemoryPetService(IEnumerable<Pet> pets)
        {
            _pets = pets.ToList();
        }

        public Pet GetPet(int id)
        {
            lock (_lock)
            {
                return _pets.FirstOrDefault(p => p.Id == id)
                    ?? throw new KeyNotFoundException($"Pet {id} was not found.");
            }
        }

        public List<Pet> GetPets()
        {
            lock (_lock)
            {
                return _pets.OrderBy(p => p.Id).ToList();
            }
        }

        public List<Pet> GetAvailablePets()
        {
            return SearchPets(new PetSearchCriteria { AvailableOnly = true });
        }

        public List<Pet> SearchPets(PetSearchCriteria criteria)
        {
            ArgumentNullException.ThrowIfNull(criteria);

            var species = criteria.Species?.Trim();

            lock (_lock)
            {
                IEnumerable<Pet> query = _pets;

                if (!string.IsNullOrEmpty(species))
                {
                    query = query.Where(p => string.Equals(p.Species, species, StringComparison.OrdinalIgnoreCase));
                }

                if (criteria.MaxAge.HasValue)
                {
                    query = query.Where(p => p.Age <= criteria.MaxAge.Value);
                }

                if (criteria.AvailableOnly)
                {
                    query = query.Where(p => p.IsAvailable);
                }

                return query.OrderBy(p => p.Id).ToList();
            }
        }

        public Pet CreatePet(Pet pet)
        {
            ArgumentNullException.ThrowIfNull(pet);

            lock (_lock)
            {
                pet.Id = _pets.Count == 0 ? 1 : _pets.Max(p => p.Id) + 1;
                _pets.Add(pet);
                return pet;
            }
        }

        public void UpdatePet(Pet pet)
        {
            ArgumentNullException.ThrowIfNull(pet);

            lock (_lock)
            {
                var index = _pets.FindIndex(p => p.Id == pet.Id);
                if (index < 0)
                {
                    throw new KeyNotFoundException($"Pet {pet.Id} was not found.");
                }

                _pets[index] = pet;
            }
        }

        public void DeletePet(int id)
        {
            lock (_lock)
            {
                if (_pets.RemoveAll(p => p.Id == id) == 0)
                {
                    throw new KeyNotFoundException($"Pet {id} was not found.");
                }
            }
        }
    }
}
