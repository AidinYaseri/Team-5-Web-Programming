using Animal.Data.Context;
using Animal.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Animal.Data.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly AnimalDbContext _context;

        public UserRepository(AnimalDbContext context)
        {
            _context = context;
        }

        // Tracked on purpose: the caller can change the user and then save through IUnitOfWork.
        public Task<User?> GetByExternalIdAsync(
            string externalId,
            CancellationToken cancellationToken = default)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(externalId);

            return _context.Users.FirstOrDefaultAsync(
                user => user.ExternalId == externalId,
                cancellationToken);
        }

        public Task<bool> EmailInUseAsync(
            string normalizedEmail,
            int? exceptUserId,
            CancellationToken cancellationToken = default)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(normalizedEmail);

            var query = _context.Users
                .AsNoTracking()
                .Where(user => user.Email == normalizedEmail);

            if (exceptUserId.HasValue)
            {
                query = query.Where(user => user.Id != exceptUserId.Value);
            }

            return query.AnyAsync(cancellationToken);
        }

        public void Add(User user)
        {
            ArgumentNullException.ThrowIfNull(user);
            _context.Users.Add(user);
        }
    }
}
