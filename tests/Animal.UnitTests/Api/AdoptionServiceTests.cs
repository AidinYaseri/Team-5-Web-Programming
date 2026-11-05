using Animal.API.Contracts;
using Animal.API.Services;
using Animal.Data.Repositories;
using Animal.Domain.Entities;
using Animal.Domain.Enums;
using Animal.Domain.Exceptions;
using Animal.Domain.Queries;

namespace Animal.UnitTests.Api
{
    // Tests the orchestration in AdoptionService with fakes: rules are applied, notifications are staged,
    // and everything is saved exactly once (or not at all when a rule fails).
    public class AdoptionServiceTests
    {
        private static readonly DateTimeOffset Now = new(2026, 11, 5, 12, 0, 0, TimeSpan.Zero);

        private readonly FakeAdoptionRepository _adoptions = new();
        private readonly FakePetRepository _pets = new();
        private readonly FakeNotificationService _notifications = new();
        private readonly FakeUnitOfWork _unitOfWork = new();
        private readonly AdoptionService _service;

        public AdoptionServiceTests()
        {
            _pets.Items.Add(new Pet { Id = 1, Name = "Biscuit", Species = "Dog", Breed = "Beagle", Age = 3, IsAvailable = true, ShelterId = 1 });
            _pets.Items.Add(new Pet { Id = 3, Name = "Rex", Species = "Dog", Breed = "German Shepherd", Age = 7, IsAvailable = false, ShelterId = 2 });
            _service = new AdoptionService(_adoptions, _pets, _notifications, _unitOfWork, new FixedTimeProvider(Now));
        }

        [Fact]
        public async Task ApplyAsync_AvailablePet_AddsPendingNotifiesAndSavesOnce()
        {
            var result = await _service.ApplyAsync(51, new CreateAdoptionRequest { PetId = 1 });

            var added = Assert.Single(_adoptions.Items);
            Assert.Equal(AdoptionStatus.Pending, added.Status);
            Assert.Equal(51, added.UserId);
            Assert.Equal(Now.UtcDateTime, added.ApplicationDate);
            Assert.Equal("Biscuit", result.PetName);
            Assert.Equal("Pending", result.Status);
            Assert.Single(_notifications.Created, n => n.UserId == 51);
            Assert.Equal(1, _unitOfWork.SaveCount);
        }

        [Fact]
        public async Task ApplyAsync_UnavailablePet_ThrowsConflictAndSavesNothing()
        {
            await Assert.ThrowsAsync<EntityConflictException>(() => _service.ApplyAsync(51, new CreateAdoptionRequest { PetId = 3 }));

            Assert.Empty(_adoptions.Items);
            Assert.Empty(_notifications.Created);
            Assert.Equal(0, _unitOfWork.SaveCount);
        }

        [Fact]
        public async Task ApplyAsync_AlreadyPending_ThrowsConflictAndSavesNothing()
        {
            _adoptions.Items.Add(new AdoptionApplication { Id = 9, UserId = 51, PetId = 1, Status = AdoptionStatus.Pending });

            await Assert.ThrowsAsync<EntityConflictException>(() => _service.ApplyAsync(51, new CreateAdoptionRequest { PetId = 1 }));

            Assert.Equal(0, _unitOfWork.SaveCount);
        }

        [Fact]
        public async Task ApplyAsync_UnknownPet_ThrowsNotFound()
        {
            await Assert.ThrowsAsync<EntityNotFoundException>(() => _service.ApplyAsync(51, new CreateAdoptionRequest { PetId = 999 }));
        }

        [Fact]
        public async Task ApproveAsync_ApprovesRejectsOthersNotifiesAllAndSavesOnce()
        {
            _adoptions.Items.Add(new AdoptionApplication { Id = 1, UserId = 51, PetId = 1, Status = AdoptionStatus.Pending });
            _adoptions.Items.Add(new AdoptionApplication { Id = 2, UserId = 52, PetId = 1, Status = AdoptionStatus.Pending });
            _adoptions.Items.Add(new AdoptionApplication { Id = 3, UserId = 53, PetId = 1, Status = AdoptionStatus.Rejected });

            await _service.ApproveAsync(1);

            Assert.Equal(AdoptionStatus.Approved, _adoptions.Items[0].Status);
            Assert.Equal(AdoptionStatus.Rejected, _adoptions.Items[1].Status);
            Assert.False(_pets.Items[0].IsAvailable);
            Assert.Equal([51, 52], _notifications.Created.Select(n => n.UserId).Order());
            Assert.Equal(1, _unitOfWork.SaveCount);
        }

        [Fact]
        public async Task ApproveAsync_NotPending_ThrowsAndSavesNothing()
        {
            _adoptions.Items.Add(new AdoptionApplication { Id = 1, UserId = 51, PetId = 1, Status = AdoptionStatus.Rejected });

            await Assert.ThrowsAsync<InvalidStatusTransitionException>(() => _service.ApproveAsync(1));

            Assert.True(_pets.Items[0].IsAvailable);
            Assert.Equal(0, _unitOfWork.SaveCount);
        }

        [Fact]
        public async Task GetAsync_SomeoneElsesApplication_ReadsAsNotFoundUnlessReviewer()
        {
            _adoptions.Items.Add(new AdoptionApplication { Id = 1, UserId = 51, PetId = 1, Status = AdoptionStatus.Pending });

            await Assert.ThrowsAsync<EntityNotFoundException>(() => _service.GetAsync(1, requesterUserId: 52, requesterCanReview: false));
            Assert.Equal(1, (await _service.GetAsync(1, requesterUserId: 51, requesterCanReview: false)).Id);
            Assert.Equal(1, (await _service.GetAsync(1, requesterUserId: null, requesterCanReview: true)).Id);
        }

        private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
        {
            public override DateTimeOffset GetUtcNow() => now;
        }

        private sealed class FakeAdoptionRepository : IAdoptionRepository
        {
            public List<AdoptionApplication> Items { get; } = [];

            private static AdoptionWithPet WithName(AdoptionApplication a) => new(a, $"Pet {a.PetId}");

            public Task<AdoptionApplication?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
                Task.FromResult(Items.FirstOrDefault(a => a.Id == id));

            public Task<AdoptionWithPet?> GetWithPetAsync(int id, CancellationToken cancellationToken = default) =>
                Task.FromResult(Items.Where(a => a.Id == id).Select(WithName).FirstOrDefault());

            public Task<List<AdoptionWithPet>> GetForUserAsync(int userId, CancellationToken cancellationToken = default) =>
                Task.FromResult(Items.Where(a => a.UserId == userId).Select(WithName).ToList());

            public Task<List<AdoptionWithPet>> SearchAsync(AdoptionStatus? status, CancellationToken cancellationToken = default) =>
                Task.FromResult(Items.Where(a => status is null || a.Status == status).Select(WithName).ToList());

            public Task<bool> HasPendingAsync(int userId, int petId, CancellationToken cancellationToken = default) =>
                Task.FromResult(Items.Any(a => a.UserId == userId && a.PetId == petId && a.Status == AdoptionStatus.Pending));

            public Task<List<AdoptionApplication>> GetOtherPendingForPetAsync(int petId, int exceptApplicationId, CancellationToken cancellationToken = default) =>
                Task.FromResult(Items.Where(a => a.PetId == petId && a.Id != exceptApplicationId && a.Status == AdoptionStatus.Pending).ToList());

            public void Add(AdoptionApplication application) => Items.Add(application);
        }

        private sealed class FakePetRepository : IPetRepository
        {
            public List<Pet> Items { get; } = [];

            public Task<Pet?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
                Task.FromResult(Items.FirstOrDefault(p => p.Id == id));

            public Task<List<Pet>> SearchAsync(PetSearchCriteria criteria, CancellationToken cancellationToken = default) =>
                Task.FromResult(Items.ToList());

            public Task<bool> HasOpenApplicationAsync(int petId, CancellationToken cancellationToken = default) =>
                Task.FromResult(false);

            public Task<List<Pet>> GetAllAsync(CancellationToken cancellationToken = default) => Task.FromResult(Items.ToList());
            public void Add(Pet entity) => Items.Add(entity);
            public void Remove(Pet entity) => Items.Remove(entity);
        }

        private sealed class FakeNotificationService : INotificationService
        {
            public List<(int UserId, string Message)> Created { get; } = [];

            public Task CreateAsync(int userId, string message, CancellationToken cancellationToken = default)
            {
                Created.Add((userId, message));
                return Task.CompletedTask;
            }

            public Task<IReadOnlyList<NotificationResponse>> GetForUserAsync(int userId, bool unreadOnly, CancellationToken cancellationToken = default) =>
                Task.FromResult<IReadOnlyList<NotificationResponse>>([]);

            public Task<bool> MarkAsReadAsync(int userId, int notificationId, CancellationToken cancellationToken = default) =>
                Task.FromResult(false);
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
