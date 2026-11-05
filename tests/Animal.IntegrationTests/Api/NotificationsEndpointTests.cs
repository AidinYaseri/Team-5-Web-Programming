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
            UseTestToken(41, "notifications.read");

            var first = await _client.PostAsync("/api/notifications/101/read", content: null);
            var second = await _client.PostAsync("/api/notifications/101/read", content: null);
            var otherUser = await _client.PostAsync("/api/notifications/103/read", content: null);

            Assert.Equal(HttpStatusCode.NoContent, first.StatusCode);
            Assert.Equal(HttpStatusCode.NoContent, second.StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, otherUser.StatusCode);
        }

        // Signed in with the right scope, but the sub has no profile yet: the policy needs a mapped user, so 403 (not 500).
        [Fact]
        public async Task Get_SignedInWithoutProfile_ReturnsForbidden()
        {
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
                "Bearer",
                _factory.CreateToken(subject: "no-profile-yet", scopes: ["notifications.read"]));

            var response = await _client.GetAsync("/api/notifications");

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        // A token that brings its own local_user_id must not be trusted: the claims transformation strips it
        // and maps only from sub. Here sub has no profile, so the forged claim for user 41 gets nothing.
        [Fact]
        public async Task Get_WithForgedLocalUserIdClaim_IsIgnored()
        {
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
                "Bearer",
                _factory.CreateToken(
                    subject: "attacker-without-profile",
                    scopes: ["notifications.read"],
                    extraClaims: [new System.Security.Claims.Claim("local_user_id", "41")]));

            var response = await _client.GetAsync("/api/notifications");

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        // Even when the attacker has a profile (user 42), a forged local_user_id for user 41 is replaced by their own id.
        [Fact]
        public async Task Get_WithForgedLocalUserIdClaim_ReturnsOnlyOwnNotifications()
        {
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
                "Bearer",
                _factory.CreateToken(
                    subject: "42",
                    scopes: ["notifications.read"],
                    extraClaims: [new System.Security.Claims.Claim("local_user_id", "41")]));

            var result = await _client.GetFromJsonAsync<List<NotificationResponse>>("/api/notifications");

            Assert.NotNull(result);
            Assert.Equal([103], result.Select(notification => notification.Id));
        }

        // A real signed access token whose sub is the user's ExternalId (ResetUserData sets it to the id).
        private void UseTestToken(int userId, string scope) =>
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
                "Bearer",
                _factory.CreateToken(subject: userId.ToString(System.Globalization.CultureInfo.InvariantCulture), scopes: [scope]));

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
