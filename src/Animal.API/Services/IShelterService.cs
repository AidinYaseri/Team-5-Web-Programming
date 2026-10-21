using Animal.Domain.Entities;

namespace Animal.API.Services
{
    // Not implemented yet. The slice owner switches these to the DTOs in docs/milestone-3/api-contracts.md.
    public interface IShelterService
    {
        Task<Shelter> GetShelterAsync(int id, CancellationToken cancellationToken = default);
        Task<List<Shelter>> GetSheltersAsync(CancellationToken cancellationToken = default);
        Task<Shelter> CreateShelterAsync(Shelter shelter, CancellationToken cancellationToken = default);
        Task UpdateShelterAsync(Shelter shelter, CancellationToken cancellationToken = default);
        Task DeleteShelterAsync(int id, CancellationToken cancellationToken = default);
    }
}
