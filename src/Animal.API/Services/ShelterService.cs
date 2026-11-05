using Animal.API.Contracts;
using Animal.Data.Repositories;
using Animal.Domain.Entities;
using Animal.Domain.Exceptions;
using Animal.Domain.Services;

namespace Animal.API.Services
{
    public class ShelterService : IShelterService
    {
        private readonly IShelterRepository _shelters;
        private readonly IUnitOfWork _unitOfWork;

        public ShelterService(IShelterRepository shelters, IUnitOfWork unitOfWork)
        {
            _shelters = shelters;
            _unitOfWork = unitOfWork;
        }

        public async Task<List<ShelterResponse>> GetSheltersAsync(CancellationToken cancellationToken = default)
        {
            var shelters = await _shelters.GetAllAsync(cancellationToken);
            return shelters.OrderBy(s => s.Id).Select(ShelterResponse.FromShelter).ToList();
        }

        public async Task<ShelterResponse> GetShelterAsync(int id, CancellationToken cancellationToken = default)
        {
            var shelter = await FindShelterAsync(id, cancellationToken);
            return ShelterResponse.FromShelter(shelter);
        }

        public async Task<List<PetResponse>> GetShelterPetsAsync(int id, CancellationToken cancellationToken = default)
        {
            if (!await _shelters.ExistsAsync(id, cancellationToken))
            {
                throw new EntityNotFoundException(nameof(Shelter), id);
            }

            var pets = await _shelters.GetPetsAsync(id, cancellationToken);
            return pets.Select(PetResponse.FromPet).ToList();
        }

        public async Task<ShelterResponse> CreateShelterAsync(CreateShelterRequest request, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(request);

            var shelter = new Shelter
            {
                Name = request.Name.Trim(),
                Address = request.Address.Trim(),
                PhoneNumber = request.PhoneNumber.Trim(),
                Email = request.Email.Trim()
            };

            ShelterRules.EnsureValid(shelter);

            _shelters.Add(shelter);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return ShelterResponse.FromShelter(shelter);
        }

        public async Task UpdateShelterAsync(int id, UpdateShelterRequest request, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(request);

            var shelter = await FindShelterAsync(id, cancellationToken);

            shelter.Name = request.Name.Trim();
            shelter.Address = request.Address.Trim();
            shelter.PhoneNumber = request.PhoneNumber.Trim();
            shelter.Email = request.Email.Trim();

            ShelterRules.EnsureValid(shelter);

            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        public async Task DeleteShelterAsync(int id, CancellationToken cancellationToken = default)
        {
            var shelter = await FindShelterAsync(id, cancellationToken);

            if (await _shelters.HasPetsAsync(id, cancellationToken))
            {
                throw new EntityConflictException($"Shelter {id} can't be deleted because it still has pets.");
            }

            _shelters.Remove(shelter);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        private async Task<Shelter> FindShelterAsync(int id, CancellationToken cancellationToken)
        {
            return await _shelters.GetByIdAsync(id, cancellationToken)
                ?? throw new EntityNotFoundException(nameof(Shelter), id);
        }
    }
}