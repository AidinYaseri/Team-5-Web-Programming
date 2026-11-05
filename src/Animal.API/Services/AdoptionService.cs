using Animal.API.Contracts;
using Animal.Data.Repositories;
using Animal.Domain.Entities;
using Animal.Domain.Enums;
using Animal.Domain.Exceptions;
using Animal.Domain.Services;

namespace Animal.API.Services
{
    public class AdoptionService : IAdoptionService
    {
        private readonly IAdoptionRepository _adoptions;
        private readonly IPetRepository _pets;
        private readonly INotificationService _notifications;
        private readonly IUnitOfWork _unitOfWork;
        private readonly TimeProvider _timeProvider;

        public AdoptionService(
            IAdoptionRepository adoptions,
            IPetRepository pets,
            INotificationService notifications,
            IUnitOfWork unitOfWork,
            TimeProvider timeProvider)
        {
            _adoptions = adoptions;
            _pets = pets;
            _notifications = notifications;
            _unitOfWork = unitOfWork;
            _timeProvider = timeProvider;
        }

        public async Task<AdoptionResponse> ApplyAsync(int userId, CreateAdoptionRequest request, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(request);
            if (userId <= 0)
            {
                throw new DomainValidationException("A valid user is required to apply for a pet.");
            }

            var pet = await _pets.GetByIdAsync(request.PetId, cancellationToken)
                ?? throw new EntityNotFoundException(nameof(Pet), request.PetId);

            AdoptionRules.EnsureCanApply(pet);
            AdoptionRules.EnsureNotAlreadyApplied(await _adoptions.HasPendingAsync(userId, pet.Id, cancellationToken));

            var application = new AdoptionApplication
            {
                UserId = userId,
                PetId = pet.Id,
                Status = AdoptionStatus.Pending,
                ApplicationDate = _timeProvider.GetUtcNow().UtcDateTime
            };

            _adoptions.Add(application);
            await _notifications.CreateAsync(userId, $"We received your application to adopt {pet.Name}.", cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return AdoptionResponse.FromApplication(application, pet.Name);
        }

        public async Task<List<AdoptionResponse>> GetMineAsync(int userId, CancellationToken cancellationToken = default)
        {
            var results = await _adoptions.GetForUserAsync(userId, cancellationToken);
            return results.Select(ToResponse).ToList();
        }

        public async Task<AdoptionResponse> GetAsync(int id, int? requesterUserId, bool requesterCanReview, CancellationToken cancellationToken = default)
        {
            var result = await _adoptions.GetWithPetAsync(id, cancellationToken);

            // Someone else's application reads as "not found", so its existence isn't revealed.
            if (result is null || (!requesterCanReview && result.Application.UserId != requesterUserId))
            {
                throw new EntityNotFoundException(nameof(AdoptionApplication), id);
            }

            return ToResponse(result);
        }

        public async Task<List<AdoptionResponse>> SearchAsync(AdoptionQuery query, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(query);

            var results = await _adoptions.SearchAsync(query.Status, cancellationToken);
            return results.Select(ToResponse).ToList();
        }

        // Approve one application, reject every other pending one for the same pet, notify each applicant,
        // and save it all in one SaveChangesAsync so it either all happens or none of it does.
        public async Task ApproveAsync(int id, CancellationToken cancellationToken = default)
        {
            var application = await FindAsync(id, cancellationToken);
            var pet = await _pets.GetByIdAsync(application.PetId, cancellationToken)
                ?? throw new EntityNotFoundException(nameof(Pet), application.PetId);

            AdoptionRules.Approve(application, pet);
            await _notifications.CreateAsync(application.UserId, $"Your application to adopt {pet.Name} was approved.", cancellationToken);

            var others = await _adoptions.GetOtherPendingForPetAsync(pet.Id, application.Id, cancellationToken);
            foreach (var other in others)
            {
                AdoptionRules.Reject(other);
                await _notifications.CreateAsync(other.UserId, $"Your application to adopt {pet.Name} was not accepted because the pet was adopted.", cancellationToken);
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        public async Task RejectAsync(int id, CancellationToken cancellationToken = default)
        {
            var application = await FindAsync(id, cancellationToken);
            var pet = await _pets.GetByIdAsync(application.PetId, cancellationToken);

            AdoptionRules.Reject(application);
            await _notifications.CreateAsync(application.UserId, $"Your application to adopt {pet?.Name ?? "this pet"} was not accepted.", cancellationToken);

            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        private async Task<AdoptionApplication> FindAsync(int id, CancellationToken cancellationToken)
        {
            return await _adoptions.GetByIdAsync(id, cancellationToken)
                ?? throw new EntityNotFoundException(nameof(AdoptionApplication), id);
        }

        private static AdoptionResponse ToResponse(AdoptionWithPet result) =>
            AdoptionResponse.FromApplication(result.Application, result.PetName);
    }
}
