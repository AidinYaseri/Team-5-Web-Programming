using Animal.API.Contracts;
using Animal.Data.Repositories;
using Animal.Domain.Entities;
using Animal.Domain.Exceptions;
using Animal.Domain.Services;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;

namespace Animal.API.Services
{
    public class UserService : IUserService
    {
        private readonly IUserRepository _users;
        private readonly IProfilePictureStore _profilePictures;
        private readonly IUnitOfWork _unitOfWork;

        public UserService(IUserRepository users, IProfilePictureStore profilePictures, IUnitOfWork unitOfWork)
        {
            _users = users;
            _profilePictures = profilePictures;
            _unitOfWork = unitOfWork;
        }

        public async Task<UserResponse> GetMyProfileAsync(
            string subject,
            CancellationToken cancellationToken = default)
        {
            var user = await GetRequiredUserAsync(subject, cancellationToken);
            var hasPicture = await _profilePictures.GetAsync(user.Id, cancellationToken) is not null;

            return UserResponse.FromUser(user, hasPicture);
        }

        public async Task SaveMyProfilePictureAsync(
            string subject,
            string contentType,
            byte[] bytes,
            CancellationToken cancellationToken = default)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(contentType);
            ArgumentNullException.ThrowIfNull(bytes);

            var user = await GetRequiredUserAsync(subject, cancellationToken);
            var normalized = await NormalizeProfilePictureAsync(bytes, cancellationToken);

            await _profilePictures.SaveAsync(user.Id, normalized.ContentType, normalized.Bytes, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        public async Task<(string ContentType, byte[] Bytes)> GetMyProfilePictureAsync(
            string subject,
            CancellationToken cancellationToken = default)
        {
            var user = await GetRequiredUserAsync(subject, cancellationToken);
            var picture = await _profilePictures.GetAsync(user.Id, cancellationToken)
                ?? throw new EntityNotFoundException(nameof(ProfilePicture), "You don't have a profile picture yet.");

            return (picture.ContentType, picture.Bytes);
        }

        public async Task DeleteMyProfilePictureAsync(
            string subject,
            CancellationToken cancellationToken = default)
        {
            var user = await GetRequiredUserAsync(subject, cancellationToken);

            await _profilePictures.DeleteAsync(user.Id, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        private async Task<User> GetRequiredUserAsync(string subject, CancellationToken cancellationToken)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(subject);

            return await _users.GetByExternalIdAsync(subject, cancellationToken)
                ?? throw new EntityNotFoundException(nameof(User), "You don't have a profile yet.");
        }

        private static async Task<(string ContentType, byte[] Bytes)> NormalizeProfilePictureAsync(byte[] bytes, CancellationToken cancellationToken)
        {
            try
            {
                using var input = new MemoryStream(bytes, writable: false);
                using var image = await Image.LoadAsync(input, cancellationToken);
                using var output = new MemoryStream();
                var decodedFormat = image.Metadata.DecodedImageFormat;

                if (string.Equals(decodedFormat?.DefaultMimeType, "image/jpeg", StringComparison.OrdinalIgnoreCase))
                {
                    await image.SaveAsync(output, new JpegEncoder { Quality = 90 }, cancellationToken);
                    return ("image/jpeg", output.ToArray());
                }

                await image.SaveAsync(output, new PngEncoder(), cancellationToken);
                return ("image/png", output.ToArray());
            }
            catch (UnknownImageFormatException)
            {
                throw new DomainValidationException("The uploaded profile picture is not a valid image.");
            }
            catch (InvalidImageContentException)
            {
                throw new DomainValidationException("The uploaded profile picture is not a valid image.");
            }
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
