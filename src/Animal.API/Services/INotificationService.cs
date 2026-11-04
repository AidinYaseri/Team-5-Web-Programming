using Animal.Domain.Entities;

namespace Animal.API.Services
{
    // Planned application service, not implemented yet. When you build it, follow IPetService/PetService:
    // async methods, request/response DTOs from Contracts/ instead of entities, and repositories from Animal.Data.
    public interface INotificationService
    {
        Notification GetNotification(int id);
        List<Notification> GetNotifications();
        Notification CreateNotification(Notification notification);
        void MarkAsRead(int id);
    }
}
