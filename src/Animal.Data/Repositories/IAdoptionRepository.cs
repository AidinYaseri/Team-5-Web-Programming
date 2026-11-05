using Animal.Domain.Entities;
using Animal.Domain.Enums;

namespace Animal.Data.Repositories
{
    // Add only stages the change. Nothing is written until IUnitOfWork.SaveChangesAsync is called.
    public interface IAdoptionRepository
    {
        // Tracked: the caller changes the status and then saves.
        Task<AdoptionApplication?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

        Task<AdoptionWithPet?> GetWithPetAsync(int id, CancellationToken cancellationToken = default);
        Task<List<AdoptionWithPet>> GetForUserAsync(int userId, CancellationToken cancellationToken = default);
        Task<List<AdoptionWithPet>> SearchAsync(AdoptionStatus? status, CancellationToken cancellationToken = default);
        Task<bool> HasPendingAsync(int userId, int petId, CancellationToken cancellationToken = default);

        // Tracked: used on approve to reject everyone else's pending application for the same pet.
        Task<List<AdoptionApplication>> GetOtherPendingForPetAsync(int petId, int exceptApplicationId, CancellationToken cancellationToken = default);

        void Add(AdoptionApplication application);
    }
}
