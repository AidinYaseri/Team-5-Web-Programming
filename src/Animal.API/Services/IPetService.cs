using Animal.API.Contracts;

namespace Animal.API.Services
{
    // Application service for pets: the controller calls this, and this coordinates the repositories, domain rules and saving.
    public interface IPetService
    {
        Task<List<PetResponse>> SearchPetsAsync(PetSearchQuery query, CancellationToken cancellationToken = default);
        Task<PetResponse> GetPetAsync(int id, CancellationToken cancellationToken = default);
        Task<PetResponse> CreatePetAsync(CreatePetRequest request, CancellationToken cancellationToken = default);
        Task UpdatePetAsync(int id, UpdatePetRequest request, CancellationToken cancellationToken = default);
        Task DeletePetAsync(int id, CancellationToken cancellationToken = default);
    }
}
