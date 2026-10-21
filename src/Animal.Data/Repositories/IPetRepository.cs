using Animal.Domain.Entities;
using Animal.Domain.ValueObjects;

namespace Animal.Data.Repositories
{
    // Persistence for pets. Async so the EF Core version (WI-2) can replace the in-memory one without changing callers.
    public interface IPetRepository
    {
        Task<Pet?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
        Task<List<Pet>> GetAllAsync(CancellationToken cancellationToken = default);
        Task<List<Pet>> SearchAsync(PetSearchCriteria criteria, CancellationToken cancellationToken = default);
        Task<Pet> AddAsync(Pet pet, CancellationToken cancellationToken = default);

        // Returns false when no pet has that id.
        Task<bool> UpdateAsync(Pet pet, CancellationToken cancellationToken = default);
        Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);
    }
}
