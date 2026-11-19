using Animal.API.Contracts;
using Animal.Data.Repositories;
using Animal.Domain.Entities;
using Animal.Domain.Exceptions;
using Animal.Domain.Services;

namespace Animal.API.Services
{
    public sealed class ProfilePictureService : IProfilePictureService
    {
        private readonly IUserRepository _users;
        private readonly IProfilePictureStore _pictures;
        private readonly IUnitOfWork _unitOfWork;

        public ProfilePictureService(
            IUserRepository users,
            IProfilePictureStore pictures,
            IUnitOfWork unitOfWork)
        {
            _users = users;
            _pictures = pictures;
            _unitOfWork = unitOfWork;
        }

        public async Task<ProfilePictureContent?> GetAsync(
            string subject,
            CancellationToken cancellationToken = default)
        {
            var user = await GetUserAsync(subject, cancellationToken);
            var picture = await _pictures.GetAsync(user.Id, cancellationToken);
            return picture is null
                ? null
                : new ProfilePictureContent(picture.ContentType, picture.Bytes);
        }

        public async Task SaveAsync(
            string subject,
            string contentType,
            byte[] bytes,
            CancellationToken cancellationToken = default)
        {
            ProfilePictureRules.EnsureValid(contentType, bytes);
            var user = await GetUserAsync(subject, cancellationToken);
            await _pictures.SaveAsync(user.Id, contentType, bytes, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        public async Task DeleteAsync(
            string subject,
            CancellationToken cancellationToken = default)
        {
            var user = await GetUserAsync(subject, cancellationToken);
            await _pictures.DeleteAsync(user.Id, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        private async Task<User> GetUserAsync(string subject, CancellationToken cancellationToken)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(subject);
            return await _users.GetByExternalIdAsync(subject, cancellationToken)
                ?? throw new EntityNotFoundException(nameof(User), "You don't have a profile yet.");
        }
    }
}
