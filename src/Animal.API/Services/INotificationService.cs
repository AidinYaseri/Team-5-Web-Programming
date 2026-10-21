using Animal.Domain.Entities;

namespace Animal.API.Services
{
    // Not implemented yet. The slice owner switches these to the DTOs in docs/milestone-3/api-contracts.md.
    public interface INotificationService
    {
        Task<Notification> GetNotificationAsync(int id, CancellationToken cancellationToken = default);
        Task<List<Notification>> GetNotificationsAsync(CancellationToken cancellationToken = default);
        Task<Notification> CreateNotificationAsync(Notification notification, CancellationToken cancellationToken = default);
        Task MarkAsReadAsync(int id, CancellationToken cancellationToken = default);
    }
}
