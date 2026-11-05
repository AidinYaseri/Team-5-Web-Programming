using Animal.Data.Context;
using Animal.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Animal.Data.Repositories
{
    public sealed class UserRepository(AnimalDbContext context) : IUserRepository
    {
        public Task<User?> GetByExternalIdAsync(string externalId, CancellationToken cancellationToken = default)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(externalId);
            return context.Users.SingleOrDefaultAsync(user => user.ExternalId == externalId, cancellationToken);
        }
    }
}
