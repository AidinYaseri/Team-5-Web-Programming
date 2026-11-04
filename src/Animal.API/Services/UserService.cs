using Animal.API.Contracts;
using Animal.Data.Repositories;
using Animal.Domain.Entities;
using Animal.Domain.Exceptions;
using Animal.Domain.Services;

namespace Animal.API.Services
{
    public class UserService : IUserService
    {
        private readonly IUserRepository _users;
        private readonly IUnitOfWork _unitOfWork;

        public UserService(IUserRepository users, IUnitOfWork unitOfWork)
        {
            _users = users;
            _unitOfWork = unitOfWork;
        }

        public async Task<UserResponse> GetMyProfileAsync(
            string subject,
            CancellationToken cancellationToken = default)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(subject);

            var user = await _users.GetByExternalIdAsync(subject, cancellationToken)
                ?? throw new EntityNotFoundException(nameof(User), 0);

            return UserResponse.FromUser(user);
        }

        public async Task CreateOrUpdateMyProfileAsync(
            string subject,
            UpdateUserRequest request,
            CancellationToken cancellationToken = default)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(subject);
            ArgumentNullException.ThrowIfNull(request);

            var user = await _users.GetByExternalIdAsync(subject, cancellationToken);
            if (user is null)
            {
                user = new User
                {
                    ExternalId = subject
                };
                _users.Add(user);
            }

            user.Name = request.Name.Trim();
            user.Email = request.Email.Trim().ToLowerInvariant();
            user.PhoneNumber = request.PhoneNumber.Trim();

            UserRules.EnsureValid(user);

            if (await _users.EmailInUseAsync(user.Email, user.Id, cancellationToken))
            {
                throw new DuplicateValueException(nameof(User.Email));
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }
}
