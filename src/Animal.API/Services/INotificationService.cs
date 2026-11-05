using Animal.API.Contracts;

namespace Animal.API.Services
{
    public interface INotificationService
    {
        Task<IReadOnlyList<NotificationResponse>> GetForUserAsync(int userId, bool unreadOnly, CancellationToken cancellationToken = default);
        Task CreateAsync(int userId, string message, CancellationToken cancellationToken = default);
        Task<bool> MarkAsReadAsync(int userId, int notificationId, CancellationToken cancellationToken = default);
    }
}
