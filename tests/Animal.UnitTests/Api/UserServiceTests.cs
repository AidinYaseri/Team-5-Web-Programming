using Animal.API.Contracts;
using Animal.API.Services;
using Animal.Data.Repositories;
using Animal.Domain.Entities;
using Animal.Domain.Exceptions;

namespace Animal.UnitTests.Api
{
    public class UserServiceTests
    {
        [Fact]
        public async Task GetMyProfileAsync_ExistingUser_ReturnsDto()
        {
            var users = new FakeUserRepository();
            users.Items.Add(new User { Id = 7, ExternalId = "subject-7", Name = "Sam", Email = "sam@example.com", PhoneNumber = "555-0100" });
            var service = new UserService(users, new FakeUnitOfWork());

            var result = await service.GetMyProfileAsync("subject-7");

            Assert.Equal(new UserResponse(7, "Sam", "sam@example.com", "555-0100"), result);
        }

        [Fact]
        public async Task GetMyProfileAsync_MissingUser_ThrowsNotFound()
        {
            var service = new UserService(new FakeUserRepository(), new FakeUnitOfWork());

            await Assert.ThrowsAsync<EntityNotFoundException>(() => service.GetMyProfileAsync("missing"));
        }

        [Fact]
        public async Task CreateOrUpdateMyProfileAsync_NewUser_NormalizesAddsAndSavesOnce()
        {
            var users = new FakeUserRepository();
            var unitOfWork = new FakeUnitOfWork();
            var service = new UserService(users, unitOfWork);

            await service.CreateOrUpdateMyProfileAsync(
                "subject-new",
                new UpdateUserRequest { Name = "  Sam  ", Email = "  Sam@Example.com ", PhoneNumber = " 555-0100 " });

            var user = Assert.Single(users.Items);
            Assert.Equal("subject-new", user.ExternalId);
            Assert.Equal("Sam", user.Name);
            Assert.Equal("sam@example.com", user.Email);
            Assert.Equal("555-0100", user.PhoneNumber);
            Assert.Equal(1, unitOfWork.SaveCount);
        }

        [Fact]
        public async Task CreateOrUpdateMyProfileAsync_ExistingUser_AllowsOwnEmailAndSavesOnce()
        {
            var users = new FakeUserRepository();
            users.Items.Add(new User { Id = 3, ExternalId = "subject-existing", Name = "Old", Email = "old@example.com", PhoneNumber = "555-0100" });
            var unitOfWork = new FakeUnitOfWork();
            var service = new UserService(users, unitOfWork);

            await service.CreateOrUpdateMyProfileAsync(
                "subject-existing",
                new UpdateUserRequest { Name = "New", Email = " OLD@EXAMPLE.COM ", PhoneNumber = "555-0101" });

            Assert.Equal("new", users.Items.Single().Name.ToLowerInvariant());
            Assert.Equal("old@example.com", users.Items.Single().Email);
            Assert.Equal(1, unitOfWork.SaveCount);
        }

        [Fact]
        public async Task CreateOrUpdateMyProfileAsync_DuplicateEmail_ThrowsAndDoesNotSave()
        {
            var users = new FakeUserRepository();
            users.Items.Add(new User { Id = 1, ExternalId = "one", Name = "One", Email = "taken@example.com", PhoneNumber = "555-0100" });
            var unitOfWork = new FakeUnitOfWork();
            var service = new UserService(users, unitOfWork);

            await Assert.ThrowsAsync<DuplicateValueException>(() => service.CreateOrUpdateMyProfileAsync(
                "two",
                new UpdateUserRequest { Name = "Two", Email = " TAKEN@EXAMPLE.COM ", PhoneNumber = "555-0101" }));

            Assert.Equal(0, unitOfWork.SaveCount);
        }

        [Fact]
        public async Task CreateOrUpdateMyProfileAsync_InvalidProfile_ThrowsAndDoesNotSave()
        {
            var users = new FakeUserRepository();
            var unitOfWork = new FakeUnitOfWork();
            var service = new UserService(users, unitOfWork);

            await Assert.ThrowsAsync<DomainValidationException>(() => service.CreateOrUpdateMyProfileAsync(
                "subject-invalid",
                new UpdateUserRequest { Name = "", Email = "bad", PhoneNumber = "555-0100" }));

            Assert.Equal(0, unitOfWork.SaveCount);
        }

        private sealed class FakeUserRepository : IUserRepository
        {
            public List<User> Items { get; } = [];

            public Task<User?> GetByExternalIdAsync(string externalId, CancellationToken cancellationToken = default) =>
                Task.FromResult(Items.SingleOrDefault(user => user.ExternalId == externalId));

            public Task<bool> EmailInUseAsync(string normalizedEmail, int? exceptUserId, CancellationToken cancellationToken = default) =>
                Task.FromResult(Items.Any(user => user.Email == normalizedEmail && user.Id != exceptUserId));

            public void Add(User user)
            {
                user.Id = Items.Count == 0 ? 1 : Items.Max(existing => existing.Id) + 1;
                Items.Add(user);
            }
        }

        private sealed class FakeUnitOfWork : IUnitOfWork
        {
            public int SaveCount { get; private set; }

            public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
            {
                SaveCount++;
                return Task.FromResult(1);
            }
        }
    }
}
