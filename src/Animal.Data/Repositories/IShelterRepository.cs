using Animal.Domain.Entities;

namespace Animal.Data.Repositories
{
    public interface IShelterRepository : IGenericRepository<Shelter>
    {
        Task<Shelter?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
        Task<List<Pet>> GetPetsAsync(int shelterId, CancellationToken cancellationToken = default);
        Task<bool> ExistsAsync(int id, CancellationToken cancellationToken = default);
        Task<bool> HasPetsAsync(int id, CancellationToken cancellationToken = default);
    }
}
