using Animal.API.Contracts;
using Animal.Data.Repositories;
using Animal.Domain.Entities;
using Animal.Domain.Exceptions;
using Animal.Domain.Queries;
using Animal.Domain.Services;

namespace Animal.API.Services
{
    public class PetService : IPetService
    {
        private readonly IPetRepository _pets;
        private readonly IShelterRepository _shelters;
        private readonly IUnitOfWork _unitOfWork;

        public PetService(IPetRepository pets, IShelterRepository shelters, IUnitOfWork unitOfWork)
        {
            _pets = pets;
            _shelters = shelters;
            _unitOfWork = unitOfWork;
        }

        public async Task<List<PetResponse>> SearchPetsAsync(PetSearchQuery query, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(query);

            var criteria = new PetSearchCriteria
            {
                Species = query.Species,
                MaxAge = query.MaxAge,
                AvailableOnly = query.AvailableOnly
            };

            var pets = await _pets.SearchAsync(criteria, cancellationToken);
            return pets.Select(PetResponse.FromPet).ToList();
        }

        public async Task<PetResponse> GetPetAsync(int id, CancellationToken cancellationToken = default)
        {
            var pet = await FindPetAsync(id, cancellationToken);
            return PetResponse.FromPet(pet);
        }

        public async Task<PetResponse> CreatePetAsync(CreatePetRequest request, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(request);

            var pet = new Pet
            {
                Name = request.Name.Trim(),
                Species = request.Species.Trim(),
                Breed = request.Breed?.Trim() ?? string.Empty,
                Age = request.Age,
                IsAvailable = request.IsAvailable,
                ShelterId = request.ShelterId
            };

            await EnsureValidAsync(pet, cancellationToken);

            _pets.Add(pet);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return PetResponse.FromPet(pet);
        }

        public async Task UpdatePetAsync(int id, UpdatePetRequest request, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(request);

            var pet = await FindPetAsync(id, cancellationToken);

            pet.Name = request.Name.Trim();
            pet.Species = request.Species.Trim();
            pet.Breed = request.Breed?.Trim() ?? string.Empty;
            pet.Age = request.Age;
            pet.IsAvailable = request.IsAvailable;
            pet.ShelterId = request.ShelterId;

            await EnsureValidAsync(pet, cancellationToken);

            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        public async Task DeletePetAsync(int id, CancellationToken cancellationToken = default)
        {
            var pet = await FindPetAsync(id, cancellationToken);

            _pets.Remove(pet);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        private async Task<Pet> FindPetAsync(int id, CancellationToken cancellationToken)
        {
            return await _pets.GetByIdAsync(id, cancellationToken)
                ?? throw new EntityNotFoundException(nameof(Pet), id);
        }

        // Domain rules first (pure, no database), then the check that needs the database.
        private async Task EnsureValidAsync(Pet pet, CancellationToken cancellationToken)
        {
            PetRules.EnsureValid(pet);

            if (!await _shelters.ExistsAsync(pet.ShelterId, cancellationToken))
            {
                throw new DomainValidationException($"Shelter {pet.ShelterId} does not exist.");
            }
        }
    }
}
