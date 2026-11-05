using Animal.Data.Context;
using Animal.Domain.Entities;
using Animal.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Animal.Data.Repositories
{
    public class AdoptionRepository : IAdoptionRepository
    {
        private readonly AnimalDbContext _context;

        public AdoptionRepository(AnimalDbContext context)
        {
            _context = context;
        }

        public Task<AdoptionApplication?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            return _context.AdoptionApplications.FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
        }

        public Task<AdoptionWithPet?> GetWithPetAsync(int id, CancellationToken cancellationToken = default)
        {
            return WithPetNames(_context.AdoptionApplications.AsNoTracking().Where(a => a.Id == id), newestFirst: false)
                .FirstOrDefaultAsync(cancellationToken);
        }

        public Task<List<AdoptionWithPet>> GetForUserAsync(int userId, CancellationToken cancellationToken = default)
        {
            var query = _context.AdoptionApplications.AsNoTracking().Where(a => a.UserId == userId);
            return WithPetNames(query, newestFirst: true).ToListAsync(cancellationToken);
        }

        public Task<List<AdoptionWithPet>> SearchAsync(AdoptionStatus? status, CancellationToken cancellationToken = default)
        {
            IQueryable<AdoptionApplication> query = _context.AdoptionApplications.AsNoTracking();
            if (status.HasValue)
            {
                query = query.Where(a => a.Status == status.Value);
            }

            // Oldest first, so reviewers handle applications in the order they arrived.
            return WithPetNames(query, newestFirst: false).ToListAsync(cancellationToken);
        }

        public Task<bool> HasPendingAsync(int userId, int petId, CancellationToken cancellationToken = default)
        {
            return _context.AdoptionApplications.AsNoTracking()
                .AnyAsync(a => a.UserId == userId && a.PetId == petId && a.Status == AdoptionStatus.Pending, cancellationToken);
        }

        public Task<List<AdoptionApplication>> GetOtherPendingForPetAsync(int petId, int exceptApplicationId, CancellationToken cancellationToken = default)
        {
            return _context.AdoptionApplications
                .Where(a => a.PetId == petId && a.Id != exceptApplicationId && a.Status == AdoptionStatus.Pending)
                .ToListAsync(cancellationToken);
        }

        public void Add(AdoptionApplication application)
        {
            ArgumentNullException.ThrowIfNull(application);
            _context.AdoptionApplications.Add(application);
        }

        // There are no navigation properties, so the pet name comes from a join on PetId. The sort happens before
        // the record is built: EF Core can't translate an OrderBy on a member of a constructed record.
        private IQueryable<AdoptionWithPet> WithPetNames(IQueryable<AdoptionApplication> applications, bool newestFirst)
        {
            var joined = applications.Join(
                _context.Pets.AsNoTracking(),
                application => application.PetId,
                pet => pet.Id,
                (application, pet) => new { Application = application, PetName = pet.Name });

            var ordered = newestFirst
                ? joined.OrderByDescending(x => x.Application.ApplicationDate).ThenByDescending(x => x.Application.Id)
                : joined.OrderBy(x => x.Application.ApplicationDate).ThenBy(x => x.Application.Id);

            return ordered.Select(x => new AdoptionWithPet(x.Application, x.PetName));
        }
    }
}
