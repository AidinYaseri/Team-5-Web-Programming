using Animal.Data.Context;
using Animal.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Animal.Data.Repositories
{
    public sealed class NotificationRepository(AnimalDbContext context) : INotificationRepository
    {
        public async Task<IReadOnlyList<Notification>> GetForUserAsync(int userId, bool unreadOnly, CancellationToken cancellationToken = default)
        {
            var query = context.Notifications.AsNoTracking().Where(n => n.UserId == userId);
            if (unreadOnly)
            {
                query = query.Where(n => !n.IsRead);
            }

            return await query.OrderByDescending(n => n.Date).ThenByDescending(n => n.Id).ToListAsync(cancellationToken);
        }

        public Task<Notification?> GetForUserAsync(int userId, int notificationId, CancellationToken cancellationToken = default) =>
            context.Notifications.FirstOrDefaultAsync(n => n.Id == notificationId && n.UserId == userId, cancellationToken);

        public void Add(Notification notification) => context.Notifications.Add(notification);
    }
}
