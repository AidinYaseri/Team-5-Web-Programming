using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Animal.Domain.Entities;

namespace Animal.IntegrationTests.Api
{
    public sealed class NotificationsEndpointTests : IClassFixture<AnimalApiFactory>
    {
        private readonly AnimalApiFactory _factory;
        private readonly HttpClient _client;

        public NotificationsEndpointTests(AnimalApiFactory factory)
        {
            _factory = factory;
            factory.ResetUserData(
                [User(41), User(42)],
                [
                    new Notification { Id = 101, UserId = 41, Message = "Your visit is booked.", Date = new DateTime(2026, 1, 2, 0, 0, 0, DateTimeKind.Utc) },
                    new Notification { Id = 102, UserId = 41, Message = "Your application changed.", Date = new DateTime(2026, 1, 3, 0, 0, 0, DateTimeKind.Utc), IsRead = true },
                    new Notification { Id = 103, UserId = 42, Message = "A private message.", Date = new DateTime(2026, 1, 4, 0, 0, 0, DateTimeKind.Utc) }
                ]);
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task Get_WithoutToken_ReturnsUnauthorized()
        {
            var response = await _client.GetAsync("/api/notifications");

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task Get_WithoutReadScope_ReturnsForbidden()
        {
            UseTestToken(41, "appointments.read");

            var response = await _client.GetAsync("/api/notifications");

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task Get_ReturnsOnlyCurrentUsersNotificationsNewestFirst()
        {
            UseTestToken(41, "notifications.read");

            var result = await _client.GetFromJsonAsync<List<NotificationResponse>>("/api/notifications");

            Assert.NotNull(result);
            Assert.Equal([102, 101], result.Select(notification => notification.Id));
            Assert.Equal("Your application changed.", result[0].Message);
        }

        [Fact]
        public async Task Get_UnreadOnly_ReturnsOnlyUnreadNotifications()
        {
            UseTestToken(41, "notifications.read");

            var result = await _client.GetFromJsonAsync<List<NotificationResponse>>("/api/notifications?unreadOnly=true");

            Assert.NotNull(result);
            Assert.Equal([101], result.Select(notification => notification.Id));
        }

        [Fact]
        public async Task MarkAsRead_CanBeCalledTwiceAndOtherUsersRowIsHidden()
        {
            UseTestToken(41, "notifications.write");

            var first = await _client.PostAsync("/api/notifications/101/read", content: null);
            var second = await _client.PostAsync("/api/notifications/101/read", content: null);
            var otherUser = await _client.PostAsync("/api/notifications/103/read", content: null);

            Assert.Equal(HttpStatusCode.NoContent, first.StatusCode);
            Assert.Equal(HttpStatusCode.NoContent, second.StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, otherUser.StatusCode);
        }

        private void UseTestToken(int userId, string scope) =>
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(TestAuthenticationHandler.SchemeName, $"{userId} {scope}");

        private static User User(int id) => new()
        {
            Id = id,
            Name = $"Test User {id}",
            Email = $"user{id}@example.test",
            PhoneNumber = "555-0100"
        };

        private sealed record NotificationResponse(int Id, string Message, DateTime Date, bool IsRead);
    }
}
