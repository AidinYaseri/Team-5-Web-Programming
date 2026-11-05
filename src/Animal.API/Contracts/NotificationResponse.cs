using Animal.Domain.Entities;

namespace Animal.API.Contracts
{
    public record NotificationResponse(int Id, string Message, DateTime Date, bool IsRead)
    {
        public static NotificationResponse FromEntity(Notification notification) =>
            new(notification.Id, notification.Message, notification.Date, notification.IsRead);
    }
}
