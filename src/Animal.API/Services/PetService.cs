using Animal.API.Contracts;
using Animal.Data.Repositories;
using Animal.Domain.Exceptions;
using Animal.Domain.ValueObjects;

namespace Animal.API.Services
{
    public class PetService : IPetService
    {
        private readonly IPetRepository _pets;

        public PetService(IPetRepository pets)
        {
            _pets = pets;
        }

        public async Task<PetResponse> GetPetAsync(int id, CancellationToken cancellationToken = default)
        {
            var pet = await _pets.GetByIdAsync(id, cancellationToken) ?? throw new EntityNotFoundException("Pet", id);
            return PetResponse.FromPet(pet);
        }

        public async Task<List<PetResponse>> GetPetsAsync(CancellationToken cancellationToken = default)
        {
            var pets = await _pets.GetAllAsync(cancellationToken);
            return pets.Select(PetResponse.FromPet).ToList();
        }

        public Task<List<PetResponse>> GetAvailablePetsAsync(CancellationToken cancellationToken = default)
        {
            return SearchPetsAsync(new PetSearchCriteria { AvailableOnly = true }, cancellationToken);
        }

        public async Task<List<PetResponse>> SearchPetsAsync(PetSearchCriteria criteria, CancellationToken cancellationToken = default)
        {
            var pets = await _pets.SearchAsync(criteria, cancellationToken);
            return pets.Select(PetResponse.FromPet).ToList();
        }

        public async Task<PetResponse> CreatePetAsync(CreatePetRequest request, CancellationToken cancellationToken = default)
        {
            var pet = await _pets.AddAsync(request.ToPet(), cancellationToken);
            return PetResponse.FromPet(pet);
        }

        public async Task<PetResponse> UpdatePetAsync(int id, UpdatePetRequest request, CancellationToken cancellationToken = default)
        {
            var pet = request.ToPet(id);
            if (!await _pets.UpdateAsync(pet, cancellationToken))
            {
                throw new EntityNotFoundException("Pet", id);
            }

            return PetResponse.FromPet(pet);
        }

        public async Task DeletePetAsync(int id, CancellationToken cancellationToken = default)
        {
            if (!await _pets.DeleteAsync(id, cancellationToken))
            {
                throw new EntityNotFoundException("Pet", id);
            }
        }
    }
}
