using Animal.API.Contracts;

namespace Animal.API.Services
{
    public interface IProfilePictureService
    {
        Task<ProfilePictureContent?> GetAsync(
            string subject,
            CancellationToken cancellationToken = default);

        Task SaveAsync(
            string subject,
            string contentType,
            byte[] bytes,
            CancellationToken cancellationToken = default);

        Task DeleteAsync(
            string subject,
            CancellationToken cancellationToken = default);
    }
}
