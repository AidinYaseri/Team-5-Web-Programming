using Animal.Domain.Entities;

namespace Animal.Data.Repositories
{
    public interface IProfilePictureStore
    {
        Task<bool> ExistsAsync(int userId, CancellationToken cancellationToken = default);
        Task<ProfilePicture?> GetAsync(int userId, CancellationToken cancellationToken = default);
        Task SaveAsync(int userId, string contentType, byte[] bytes, CancellationToken cancellationToken = default);
        Task DeleteAsync(int userId, CancellationToken cancellationToken = default);
    }
}
