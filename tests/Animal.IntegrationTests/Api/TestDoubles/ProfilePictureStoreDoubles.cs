using Animal.Data.Context;
using Animal.Data.Repositories;
using Animal.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Animal.IntegrationTests.Api.TestDoubles
{
    public sealed class ThrowingProfilePictureStore : IProfilePictureStore
    {
        private readonly AnimalDbContext _context;

        public ThrowingProfilePictureStore(AnimalDbContext context)
        {
            _context = context;
        }

        public Task<bool> ExistsAsync(int userId, CancellationToken cancellationToken = default) =>
            _context.ProfilePictures.AsNoTracking().AnyAsync(picture => picture.UserId == userId, cancellationToken);

        public Task<ProfilePicture?> GetAsync(int userId, CancellationToken cancellationToken = default) =>
            _context.ProfilePictures.AsNoTracking().FirstOrDefaultAsync(picture => picture.UserId == userId, cancellationToken);

        public Task SaveAsync(int userId, string contentType, byte[] bytes, CancellationToken cancellationToken = default)
        {
            throw new InvalidOperationException("Server=tcp://secret-db;User Id=sa;Password=P@ssw0rd; SELECT * FROM ProfilePictures; C:\\secrets\\upload.log");
        }

        public Task DeleteAsync(int userId, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }

    public sealed class MutatingProfilePictureStore : IProfilePictureStore
    {
        private readonly AnimalDbContext _context;

        public MutatingProfilePictureStore(AnimalDbContext context)
        {
            _context = context;
        }

        public Task<bool> ExistsAsync(int userId, CancellationToken cancellationToken = default) =>
            _context.ProfilePictures.AsNoTracking().AnyAsync(picture => picture.UserId == userId, cancellationToken);

        public Task<ProfilePicture?> GetAsync(int userId, CancellationToken cancellationToken = default) =>
            _context.ProfilePictures.AsNoTracking().FirstOrDefaultAsync(picture => picture.UserId == userId, cancellationToken);

        public async Task SaveAsync(int userId, string contentType, byte[] bytes, CancellationToken cancellationToken = default)
        {
            var existing = await _context.ProfilePictures.FindAsync([userId], cancellationToken);
            if (existing is null)
            {
                _context.ProfilePictures.Add(new ProfilePicture
                {
                    UserId = userId,
                    ContentType = contentType,
                    Bytes = bytes
                });
            }
            else
            {
                existing.ContentType = contentType;
                existing.Bytes = bytes;
            }

            var user = await _context.Users.FirstAsync(row => row.Id == userId, cancellationToken);
            user.Name = "Mutated During Upload";
        }

        public async Task DeleteAsync(int userId, CancellationToken cancellationToken = default)
        {
            var existing = await _context.ProfilePictures.FindAsync([userId], cancellationToken);
            if (existing is not null)
            {
                _context.ProfilePictures.Remove(existing);
            }
        }
    }
}
