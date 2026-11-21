using Animal.API.Contracts;
using Animal.Data.Repositories;
using Animal.Domain.Entities;
using Animal.Domain.Exceptions;
using Animal.Domain.Services;
using SkiaSharp;

namespace Animal.API.Services
{
    public class UserService : IUserService
    {
        private const int MaxProfilePictureDimension = 2048;

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
            var hasPicture = await _profilePictures.ExistsAsync(user.Id, cancellationToken);

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

            ProfilePictureRules.EnsureValid(contentType, bytes);

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

        private static Task<(string ContentType, byte[] Bytes)> NormalizeProfilePictureAsync(byte[] bytes, CancellationToken cancellationToken)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                var sourceContentType = ProfilePictureRules.DetectContentType(bytes)
                    ?? throw new DomainValidationException("The uploaded profile picture is not a valid image.");

                using var infoStream = new MemoryStream(bytes, writable: false);
                using var codec = SKCodec.Create(infoStream)
                    ?? throw new DomainValidationException("The uploaded profile picture is not a valid image.");
                var info = codec.Info;

                if (info.Width > MaxProfilePictureDimension || info.Height > MaxProfilePictureDimension)
                {
                    throw new DomainValidationException($"Profile picture dimensions can't exceed {MaxProfilePictureDimension}x{MaxProfilePictureDimension} pixels.");
                }

                cancellationToken.ThrowIfCancellationRequested();

                using var input = new MemoryStream(bytes, writable: false);
                using var bitmap = SKBitmap.Decode(input)
                    ?? throw new DomainValidationException("The uploaded profile picture is not a valid image.");
                using var image = SKImage.FromBitmap(bitmap);
                using var encoded = string.Equals(sourceContentType, "image/jpeg", StringComparison.OrdinalIgnoreCase)
                    ? image.Encode(SKEncodedImageFormat.Jpeg, 90)
                    : image.Encode(SKEncodedImageFormat.Png, 100);

                if (encoded is null)
                {
                    throw new DomainValidationException("The uploaded profile picture is not a valid image.");
                }

                return Task.FromResult((sourceContentType, encoded.ToArray()));
            }
            catch (ArgumentException)
            {
                throw new DomainValidationException("The uploaded profile picture is not a valid image.");
            }
            catch (NotSupportedException)
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
