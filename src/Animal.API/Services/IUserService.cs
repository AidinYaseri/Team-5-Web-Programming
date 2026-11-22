using Animal.API.Contracts;

namespace Animal.API.Services
{
    public interface IUserService
    {
        Task<UserResponse> GetMyProfileAsync(
            string subject,
            CancellationToken cancellationToken = default);

        Task SaveMyProfilePictureAsync(
            string subject,
            string contentType,
            byte[] bytes,
            CancellationToken cancellationToken = default);

        Task<(string ContentType, byte[] Bytes)> GetMyProfilePictureAsync(
            string subject,
            CancellationToken cancellationToken = default);

        Task DeleteMyProfilePictureAsync(
            string subject,
            CancellationToken cancellationToken = default);

        Task CreateOrUpdateMyProfileAsync(
            string subject,
            UpdateUserRequest request,
            CancellationToken cancellationToken = default);
    }
}
