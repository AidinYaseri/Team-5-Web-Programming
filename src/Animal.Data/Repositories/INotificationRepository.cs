using Animal.Domain.Entities;

namespace Animal.Data.Repositories
{
    public interface INotificationRepository
    {
        Task<IReadOnlyList<Notification>> GetForUserAsync(int userId, bool unreadOnly, CancellationToken cancellationToken = default);
        Task<Notification?> GetForUserAsync(int userId, int notificationId, CancellationToken cancellationToken = default);
        void Add(Notification notification);
    }
}
