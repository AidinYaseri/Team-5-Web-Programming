using Animal.Data.Context;
using Animal.Domain.Entities;
using Animal.Domain.Queries;
using Microsoft.EntityFrameworkCore;

namespace Animal.Data.Repositories
{
    public class PetRepository : IPetRepository
    {
        private readonly AnimalDbContext _context;

        public PetRepository(AnimalDbContext context)
        {
            _context = context;
        }

        // Tracked on purpose: the caller can change the pet and then save through IUnitOfWork.
        public Task<Pet?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            return _context.Pets.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
        }

        public Task<List<Pet>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            return _context.Pets.AsNoTracking().OrderBy(p => p.Id).ToListAsync(cancellationToken);
        }

        public Task<List<Pet>> SearchAsync(PetSearchCriteria criteria, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(criteria);

            IQueryable<Pet> query = _context.Pets.AsNoTracking();

            var species = criteria.Species?.Trim();
            if (!string.IsNullOrEmpty(species))
            {
                // ToLower on both sides so the match ignores case on every provider, not just SQL Server's default collation.
                var lowered = species.ToLower();
                query = query.Where(p => p.Species.ToLower() == lowered);
            }

            if (criteria.MaxAge.HasValue)
            {
                query = query.Where(p => p.Age <= criteria.MaxAge.Value);
            }

            if (criteria.AvailableOnly)
            {
                query = query.Where(p => p.IsAvailable);
            }

            return query.OrderBy(p => p.Id).ToListAsync(cancellationToken);
        }

        public void Add(Pet pet)
        {
            ArgumentNullException.ThrowIfNull(pet);
            _context.Pets.Add(pet);
        }

        public void Remove(Pet pet)
        {
            ArgumentNullException.ThrowIfNull(pet);
            _context.Pets.Remove(pet);
        }
    }
}
