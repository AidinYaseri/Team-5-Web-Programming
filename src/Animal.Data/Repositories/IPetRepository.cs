using Animal.Domain.Entities;
using Animal.Domain.Queries;

namespace Animal.Data.Repositories
{
    // Add and Remove only stage the change. Nothing is written until IUnitOfWork.SaveChangesAsync is called.
    public interface IPetRepository : IGenericRepository<Pet>
    {
        Task<Pet?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
        Task<List<Pet>> SearchAsync(PetSearchCriteria criteria, CancellationToken cancellationToken = default);
        Task<bool> HasOpenApplicationAsync(int petId, CancellationToken cancellationToken = default);
    }
}
