using Animal.Data.Repositories;
using Animal.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Animal.IntegrationTests.Data
{
    public class UserRepositoryTests : IDisposable
    {
        private readonly SqliteTestDatabase _database = new();

        public void Dispose() => _database.Dispose();

        [Fact]
        public async Task GetByExternalIdAsync_ExistingUser_ReturnsTrackedUser()
        {
            using var context = _database.CreateContext();
            context.Users.Add(new User { ExternalId = "subject-tracked", Name = "Sam", Email = "tracked@example.com", PhoneNumber = "555-0100" });
            await context.SaveChangesAsync();

            var user = await new UserRepository(context).GetByExternalIdAsync("subject-tracked");

            Assert.NotNull(user);
            user.Name = "Changed";
            await context.SaveChangesAsync();

            using var check = _database.CreateContext();
            Assert.Equal("Changed", (await check.Users.SingleAsync(u => u.ExternalId == "subject-tracked")).Name);
        }

        [Fact]
        public async Task GetByExternalIdAsync_MissingUser_ReturnsNull()
        {
            using var context = _database.CreateContext();

            Assert.Null(await new UserRepository(context).GetByExternalIdAsync("missing"));
        }

        [Fact]
        public async Task EmailInUseAsync_ExcludesRequestedUser()
        {
            using var context = _database.CreateContext();
            context.Users.AddRange(
                new User { ExternalId = "subject-one", Name = "One", Email = "same@example.com", PhoneNumber = "555-0100" },
                new User { ExternalId = "subject-two", Name = "Two", Email = "other@example.com", PhoneNumber = "555-0101" });
            await context.SaveChangesAsync();

            var repository = new UserRepository(context);
            var user = await context.Users.SingleAsync(u => u.ExternalId == "subject-one");

            Assert.False(await repository.EmailInUseAsync("same@example.com", user.Id));
            Assert.True(await repository.EmailInUseAsync("same@example.com", null));
        }

        [Fact]
        public async Task Add_IsOnlySavedAfterSaveChanges()
        {
            using var context = _database.CreateContext();
            var repository = new UserRepository(context);
            repository.Add(new User { ExternalId = "subject-added", Name = "Added", Email = "added@example.com", PhoneNumber = "555-0100" });

            using (var other = _database.CreateContext())
            {
                Assert.False(await other.Users.AnyAsync(u => u.ExternalId == "subject-added"));
            }

            await context.SaveChangesAsync();

            using var check = _database.CreateContext();
            Assert.True(await check.Users.AnyAsync(u => u.ExternalId == "subject-added"));
        }
    }
}
