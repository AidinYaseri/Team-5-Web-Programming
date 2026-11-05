using Animal.Data.Context;
using Animal.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Animal.Data.Repositories
{
    public class ShelterRepository : GenericRepository<Shelter>, IShelterRepository
    {
        public ShelterRepository(AnimalDbContext context) : base(context)
        {
        }

        // Tracked on purpose so callers can edit and save through IUnitOfWork.
        public Task<Shelter?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            return Context.Shelters.FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
        }

        public Task<List<Pet>> GetPetsAsync(int shelterId, CancellationToken cancellationToken = default)
        {
            return Context.Pets
                .AsNoTracking()
                .Where(p => p.ShelterId == shelterId)
                .OrderBy(p => p.Id)
                .ToListAsync(cancellationToken);
        }

        public Task<bool> ExistsAsync(int id, CancellationToken cancellationToken = default)
        {
            return Context.Shelters.AsNoTracking().AnyAsync(s => s.Id == id, cancellationToken);
        }

        public Task<bool> HasPetsAsync(int id, CancellationToken cancellationToken = default)
        {
            return Context.Pets.AsNoTracking().AnyAsync(p => p.ShelterId == id, cancellationToken);
        }
    }
}
