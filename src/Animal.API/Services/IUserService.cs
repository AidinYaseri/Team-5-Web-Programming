using Animal.Domain.Entities;

namespace Animal.API.Services
{
    // Not implemented yet. The slice owner switches these to the DTOs in docs/milestone-3/api-contracts.md.
    public interface IUserService
    {
        Task<User> GetUserAsync(int id, CancellationToken cancellationToken = default);
        Task<List<User>> GetUsersAsync(CancellationToken cancellationToken = default);
        Task<User> CreateUserAsync(User user, CancellationToken cancellationToken = default);
        Task UpdateUserAsync(User user, CancellationToken cancellationToken = default);
        Task DeleteUserAsync(int id, CancellationToken cancellationToken = default);
    }
}
