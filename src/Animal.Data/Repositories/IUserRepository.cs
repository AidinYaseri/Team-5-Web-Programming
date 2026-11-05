using Animal.Domain.Entities;

namespace Animal.Data.Repositories
{
    public interface IUserRepository
    {
        Task<User?> GetByExternalIdAsync(
            string externalId,
            CancellationToken cancellationToken = default);

        Task<bool> EmailInUseAsync(
            string normalizedEmail,
            int? exceptUserId,
            CancellationToken cancellationToken = default);

        void Add(User user);
    }
}
