using Animal.API.Contracts;

namespace Animal.API.Services
{
    public interface IShelterService
    {
        Task<List<ShelterResponse>> GetSheltersAsync(CancellationToken cancellationToken = default);
        Task<ShelterResponse> GetShelterAsync(int id, CancellationToken cancellationToken = default);
        Task<List<PetResponse>> GetShelterPetsAsync(int id, CancellationToken cancellationToken = default);
        Task<ShelterResponse> CreateShelterAsync(CreateShelterRequest request, CancellationToken cancellationToken = default);
        Task UpdateShelterAsync(int id, UpdateShelterRequest request, CancellationToken cancellationToken = default);
        Task DeleteShelterAsync(int id, CancellationToken cancellationToken = default);
    }
}
