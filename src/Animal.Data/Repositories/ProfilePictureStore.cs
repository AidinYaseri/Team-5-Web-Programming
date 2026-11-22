using Animal.Data.Context;
using Animal.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Animal.Data.Repositories
{
    public class ProfilePictureStore : IProfilePictureStore
    {
        private readonly AnimalDbContext _context;

        public ProfilePictureStore(AnimalDbContext context)
        {
            _context = context;
        }

        public Task<ProfilePicture?> GetAsync(int userId, CancellationToken cancellationToken = default)
        {
            return _context.ProfilePictures.AsNoTracking()
                .FirstOrDefaultAsync(p => p.UserId == userId, cancellationToken);
        }

        public Task<bool> ExistsAsync(int userId, CancellationToken cancellationToken = default)
        {
            return _context.ProfilePictures.AsNoTracking()
                .AnyAsync(p => p.UserId == userId, cancellationToken);
        }

        public async Task SaveAsync(int userId, string contentType, byte[] bytes, CancellationToken cancellationToken = default)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(contentType);
            ArgumentNullException.ThrowIfNull(bytes);

            var existing = await _context.ProfilePictures.FindAsync([userId], cancellationToken);

            if (existing is null)
            {
                _context.ProfilePictures.Add(new ProfilePicture
                {
                    UserId = userId,
                    ContentType = contentType,
                    Bytes = bytes,
                    UpdatedAt = DateTime.UtcNow
                });
            }
            else
            {
                existing.ContentType = contentType;
                existing.Bytes = bytes;
                existing.UpdatedAt = DateTime.UtcNow;
            }
        }

        public async Task DeleteAsync(int userId, CancellationToken cancellationToken = default)
        {
            var existing = await _context.ProfilePictures.FindAsync([userId], cancellationToken);

            if (existing is null)
            {
                return;
            }

            _context.ProfilePictures.Remove(existing);
        }
    }
}
