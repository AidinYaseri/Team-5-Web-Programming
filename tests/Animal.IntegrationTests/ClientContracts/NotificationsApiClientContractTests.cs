extern alias ClientApp;

using ClientServices = ClientApp::Animal.Client.Services;
using Animal.IntegrationTests.Api;
using Animal.Domain.Entities;

namespace Animal.IntegrationTests.ClientContracts
{
    public sealed class NotificationsApiClientContractTests : IClassFixture<AnimalApiFactory>
    {
        private readonly AnimalApiFactory _factory;
        public NotificationsApiClientContractTests(AnimalApiFactory factory) => _factory = factory;

        [Fact]
        public async Task NotificationResponse_MapsEveryFieldAndMarkReadRequestIsAccepted()
        {
            const string subject = "notification-contract-user";
            const int userId = 61;
            const int notificationId = 601;
            var date = new DateTime(2037, 4, 5, 10, 30, 0, DateTimeKind.Utc);
            _factory.ResetUserData(
            [
                new User
                {
                    Id = userId, ExternalId = subject, Name = "Contract Recipient",
                    Email = "notification-contract@example.test", PhoneNumber = "555-0184"
                }
            ],
            notifications:
            [
                new Notification
                {
                    Id = notificationId, UserId = userId, Message = "Contract test notification",
                    Date = date, IsRead = false
                }
            ]);

            using var http = _factory.CreateAuthenticatedClient(subject, ["notifications.read"]);
            var notifications = new ClientServices.NotificationsApiClient(http);

            var unread = await notifications.GetAsync(unreadOnly: true);
            var notification = Assert.Single(unread);
            Assert.Equal(notificationId, notification.Id);
            Assert.Equal("Contract test notification", notification.Message);
            Assert.Equal(date, notification.Date);
            Assert.False(notification.IsRead);

            await notifications.MarkAsReadAsync(notificationId);

            var all = await notifications.GetAsync(unreadOnly: false);
            var markedRead = Assert.Single(all);
            Assert.Equal(notificationId, markedRead.Id);
            Assert.Equal("Contract test notification", markedRead.Message);
            Assert.Equal(date, markedRead.Date);
            Assert.True(markedRead.IsRead);
            Assert.Empty(await notifications.GetAsync(unreadOnly: true));
        }
    }
}