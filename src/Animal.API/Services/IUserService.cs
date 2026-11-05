using Animal.API.Contracts;

namespace Animal.API.Services
{
    public interface IUserService
    {
        Task<UserResponse> GetMyProfileAsync(
            string subject,
            CancellationToken cancellationToken = default);

        Task CreateOrUpdateMyProfileAsync(
            string subject,
            UpdateUserRequest request,
            CancellationToken cancellationToken = default);
    }
}
