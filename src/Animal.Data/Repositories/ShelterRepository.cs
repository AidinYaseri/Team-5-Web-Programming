using Animal.Data.Context;
using Microsoft.EntityFrameworkCore;

namespace Animal.Data.Repositories
{
    public class ShelterRepository : IShelterRepository
    {
        private readonly AnimalDbContext _context;

        public ShelterRepository(AnimalDbContext context)
        {
            _context = context;
        }

        public Task<bool> ExistsAsync(int id, CancellationToken cancellationToken = default)
        {
            return _context.Shelters.AnyAsync(s => s.Id == id, cancellationToken);
        }
    }
}
