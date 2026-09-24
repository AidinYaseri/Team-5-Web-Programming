using System;
using System.Collections.Generic;
using System.Text;
using Animal.Domain.Models;

namespace Animal.Domain.Services
{
    public interface INotificationService
    {
        Notification GetNotification(int id);
        List<Notification> GetNotifications();
        Notification CreateNotification(Notification notification);
        void MarkAsRead(int id);
    }
}
