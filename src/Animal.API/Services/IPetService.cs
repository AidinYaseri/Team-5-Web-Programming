using Animal.API.Contracts;
using Animal.Domain.ValueObjects;

namespace Animal.API.Services
{
    // Application service for pets: calls the repository, applies domain rules and maps to DTOs.
    public interface IPetService
    {
        // Throws EntityNotFoundException when the id doesn't exist (same for update and delete).
        Task<PetResponse> GetPetAsync(int id, CancellationToken cancellationToken = default);
        Task<List<PetResponse>> GetPetsAsync(CancellationToken cancellationToken = default);
        Task<List<PetResponse>> GetAvailablePetsAsync(CancellationToken cancellationToken = default);
        Task<List<PetResponse>> SearchPetsAsync(PetSearchCriteria criteria, CancellationToken cancellationToken = default);
        Task<PetResponse> CreatePetAsync(CreatePetRequest request, CancellationToken cancellationToken = default);
        Task<PetResponse> UpdatePetAsync(int id, UpdatePetRequest request, CancellationToken cancellationToken = default);
        Task DeletePetAsync(int id, CancellationToken cancellationToken = default);
    }
}
