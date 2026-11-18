using Animal.Data.Repositories;
using Animal.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Animal.IntegrationTests.Data
{
    public class ProfilePictureStoreTests : IDisposable
    {
        private readonly SqliteTestDatabase _database = new();

        public void Dispose() => _database.Dispose();

        private void EnsureUsers()
        {
            using var context = _database.CreateContext();
            if (context.Users.Any())
            {
                return;
            }

            context.Users.AddRange(
                new User { Id = 1, ExternalId = "user-1", Name = "User One", Email = "user.one@example.org", PhoneNumber = "555-1100" },
                new User { Id = 2, ExternalId = "user-2", Name = "User Two", Email = "user.two@example.org", PhoneNumber = "555-1200" });
            context.SaveChanges();
        }

        [Fact]
        public async Task SaveAsync_NewPicture_CanBeRetrieved()
        {
            EnsureUsers();

            using (var context = _database.CreateContext())
            {
                var store = new ProfilePictureStore(context);
                await store.SaveAsync(1, "image/png", [0x89, 0x50, 0x4E, 0x47]);
                await context.SaveChangesAsync();
            }

            using (var context = _database.CreateContext())
            {
                var picture = await new ProfilePictureStore(context).GetAsync(1);
                Assert.NotNull(picture);
                Assert.Equal("image/png", picture.ContentType);
                Assert.Equal([0x89, 0x50, 0x4E, 0x47], picture.Bytes);
            }
        }

        [Fact]
        public async Task SaveAsync_ExistingPicture_ReplacesBytesAndContentType()
        {
            EnsureUsers();

            using (var context = _database.CreateContext())
            {
                var store = new ProfilePictureStore(context);
                await store.SaveAsync(1, "image/png", [0x89, 0x50, 0x4E, 0x47]);
                await store.SaveAsync(1, "image/jpeg", [0xFF, 0xD8, 0xFF]);
                await context.SaveChangesAsync();
            }

            using (var context = _database.CreateContext())
            {
                var picture = await new ProfilePictureStore(context).GetAsync(1);
                Assert.NotNull(picture);
                Assert.Equal("image/jpeg", picture.ContentType);
                Assert.Equal([0xFF, 0xD8, 0xFF], picture.Bytes);
            }
        }

        [Fact]
        public async Task GetAsync_MissingPicture_ReturnsNull()
        {
            EnsureUsers();

            using var context = _database.CreateContext();
            var picture = await new ProfilePictureStore(context).GetAsync(2);

            Assert.Null(picture);
        }

        [Fact]
        public async Task DeleteAsync_RemovesPicture()
        {
            EnsureUsers();

            using (var context = _database.CreateContext())
            {
                var store = new ProfilePictureStore(context);
                await store.SaveAsync(1, "image/png", [0x89, 0x50, 0x4E, 0x47]);
                await context.SaveChangesAsync();
                await store.DeleteAsync(1);
                await context.SaveChangesAsync();
            }

            using (var context = _database.CreateContext())
            {
                Assert.Null(await new ProfilePictureStore(context).GetAsync(1));
            }
        }

        [Fact]
        public async Task GetAsync_ReturnsEachUsersOwnPicture_WhenBothUsersHaveRows()
        {
            EnsureUsers();

            using (var context = _database.CreateContext())
            {
                var store = new ProfilePictureStore(context);
                await store.SaveAsync(1, "image/png", [0x89, 0x50, 0x4E, 0x47]);
                await store.SaveAsync(2, "image/jpeg", [0xFF, 0xD8, 0xFF, 0xD9]);
                await context.SaveChangesAsync();
            }

            using (var context = _database.CreateContext())
            {
                var user1Picture = await new ProfilePictureStore(context).GetAsync(1);
                var user2Picture = await new ProfilePictureStore(context).GetAsync(2);

                Assert.NotNull(user1Picture);
                Assert.Equal("image/png", user1Picture.ContentType);
                Assert.Equal([0x89, 0x50, 0x4E, 0x47], user1Picture.Bytes);

                Assert.NotNull(user2Picture);
                Assert.Equal("image/jpeg", user2Picture.ContentType);
                Assert.Equal([0xFF, 0xD8, 0xFF, 0xD9], user2Picture.Bytes);
            }
        }

        [Fact]
        public async Task DeleteAsync_MissingPicture_DoesNothing()
        {
            EnsureUsers();

            using var context = _database.CreateContext();
            var store = new ProfilePictureStore(context);
            await store.DeleteAsync(2);

            Assert.Equal(0, await context.ProfilePictures.CountAsync());
        }
    }
}
