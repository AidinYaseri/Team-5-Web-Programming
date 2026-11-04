using Animal.Domain.Entities;
using Animal.Domain.Queries;

namespace Animal.Data.Repositories
{
    // Add and Remove only stage the change. Nothing is written until IUnitOfWork.SaveChangesAsync is called.
    public interface IPetRepository
    {
        Task<Pet?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
        Task<List<Pet>> GetAllAsync(CancellationToken cancellationToken = default);
        Task<List<Pet>> SearchAsync(PetSearchCriteria criteria, CancellationToken cancellationToken = default);
        void Add(Pet pet);
        void Remove(Pet pet);
    }
}
