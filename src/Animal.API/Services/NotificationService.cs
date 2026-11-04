using Animal.API.Contracts;
using Animal.Data.Repositories;
using Animal.Domain.Entities;
using Animal.Domain.Exceptions;
using Animal.Domain.Services;

namespace Animal.API.Services
{
    public sealed class NotificationService(
        INotificationRepository notifications,
        IUnitOfWork unitOfWork,
        TimeProvider timeProvider) : INotificationService
    {
        public async Task<IReadOnlyList<NotificationResponse>> GetForUserAsync(int userId, bool unreadOnly, CancellationToken cancellationToken = default)
        {
            var results = await notifications.GetForUserAsync(userId, unreadOnly, cancellationToken);
            return results.Select(NotificationResponse.FromEntity).ToList();
        }

        public async Task CreateAsync(int userId, string message, CancellationToken cancellationToken = default)
        {
            if (userId <= 0)
            {
                throw new DomainValidationException("A valid user is required to create a notification.");
            }

            notifications.Add(new Notification
            {
                UserId = userId,
                Message = NotificationRules.ValidateMessage(message),
                Date = timeProvider.GetUtcNow().UtcDateTime,
                IsRead = false
            });

            await unitOfWork.SaveChangesAsync(cancellationToken);
        }

        public async Task<bool> MarkAsReadAsync(int userId, int notificationId, CancellationToken cancellationToken = default)
        {
            var notification = await notifications.GetForUserAsync(userId, notificationId, cancellationToken);
            if (notification is null)
            {
                return false;
            }

            if (!notification.IsRead)
            {
                notification.IsRead = true;
                await unitOfWork.SaveChangesAsync(cancellationToken);
            }

            return true;
        }
    }
}
