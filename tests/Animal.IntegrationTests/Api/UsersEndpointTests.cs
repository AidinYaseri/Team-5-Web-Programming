using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Animal.API.Contracts;
using Animal.Data.Context;
using Microsoft.Extensions.DependencyInjection;

namespace Animal.IntegrationTests.Api
{
    public class UsersEndpointTests : IClassFixture<AnimalApiFactory>
    {
        private readonly AnimalApiFactory _factory;

        public UsersEndpointTests(AnimalApiFactory factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task GetMyProfile_BeforePut_ReturnsNotFound()
        {
            var identity = NewIdentity();
            using var client = _factory.CreateAuthenticatedClient(identity.Subject, ["users.read"]);

            var response = await client.GetAsync("/api/users/me");

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
            Assert.Contains("You don't have a profile yet.", await response.Content.ReadAsStringAsync());
        }

        [Fact]
        public async Task PutThenGetMyProfile_ReturnsSavedProfileWithoutExternalId()
        {
            var identity = NewIdentity();
            using var client = _factory.CreateAuthenticatedClient(identity.Subject, ["users.read", "users.write"]);
            var request = NewRequest(identity.Email);

            var put = await client.PutAsJsonAsync("/api/users/me", request);
            var get = await client.GetAsync("/api/users/me");
            var profile = await get.Content.ReadFromJsonAsync<UserResponse>();

            Assert.Equal(HttpStatusCode.NoContent, put.StatusCode);
            Assert.Equal(HttpStatusCode.OK, get.StatusCode);
            Assert.Equal(request.Name, profile!.Name);
            Assert.Equal(identity.Email, profile.Email);
        }

        [Fact]
        public async Task PutMyProfile_Again_UpdatesExistingRowOnly()
        {
            var identity = NewIdentity();
            using var client = _factory.CreateAuthenticatedClient(identity.Subject, ["users.write"]);

            Assert.Equal(HttpStatusCode.NoContent,
                (await client.PutAsJsonAsync("/api/users/me", NewRequest(identity.Email))).StatusCode);
            var changed = NewRequest(identity.Email) with { Name = "Changed Name" };

            Assert.Equal(HttpStatusCode.NoContent,
                (await client.PutAsJsonAsync("/api/users/me", changed)).StatusCode);

            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AnimalDbContext>();
            Assert.Equal(1, context.Users.Count(user => user.ExternalId == identity.Subject));
            Assert.Equal("Changed Name", context.Users.Single(user => user.ExternalId == identity.Subject).Name);
        }

        [Fact]
        public async Task PutMyProfile_WithOwnUnchangedEmail_ReturnsNoContent()
        {
            var identity = NewIdentity();
            using var client = _factory.CreateAuthenticatedClient(identity.Subject, ["users.write"]);

            await client.PutAsJsonAsync("/api/users/me", NewRequest(identity.Email));
            var response = await client.PutAsJsonAsync("/api/users/me", NewRequest(identity.Email.ToUpperInvariant()));

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        }

        [Fact]
        public async Task PutMyProfile_WithAnotherUsersEmail_ReturnsConflict()
        {
            var first = NewIdentity();
            var second = NewIdentity();
            using var firstClient = _factory.CreateAuthenticatedClient(first.Subject, ["users.write"]);
            using var secondClient = _factory.CreateAuthenticatedClient(second.Subject, ["users.write"]);

            await firstClient.PutAsJsonAsync("/api/users/me", NewRequest(first.Email));
            var response = await secondClient.PutAsJsonAsync("/api/users/me", NewRequest(first.Email.ToUpperInvariant()));

            Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        }

        [Fact]
        public async Task TwoUsers_GetMyProfile_ReturnsOnlyTheirOwnProfile()
        {
            var first = NewIdentity();
            var second = NewIdentity();
            using var firstClient = _factory.CreateAuthenticatedClient(first.Subject, ["users.read", "users.write"]);
            using var secondClient = _factory.CreateAuthenticatedClient(second.Subject, ["users.read", "users.write"]);

            await firstClient.PutAsJsonAsync("/api/users/me", NewRequest(first.Email));
            await secondClient.PutAsJsonAsync("/api/users/me", NewRequest(second.Email));

            var firstProfile = await firstClient.GetFromJsonAsync<UserResponse>("/api/users/me");
            var secondProfile = await secondClient.GetFromJsonAsync<UserResponse>("/api/users/me");

            Assert.Equal(first.Email, firstProfile!.Email);
            Assert.Equal(second.Email, secondProfile!.Email);
            Assert.NotEqual(firstProfile.Id, secondProfile.Id);
        }

        [Fact]
        public async Task PutMyProfile_WithInvalidBody_ReturnsBadRequest()
        {
            var identity = NewIdentity();
            using var client = _factory.CreateAuthenticatedClient(identity.Subject, ["users.write"]);

            var response = await client.PutAsJsonAsync("/api/users/me",
                new { name = "", email = "not-an-email", phoneNumber = "" });

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task MyProfile_WithoutToken_ReturnsUnauthorized()
        {
            using var client = _factory.CreateClient();

            Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/users/me")).StatusCode);
            Assert.Equal(HttpStatusCode.Unauthorized,
                (await client.PutAsJsonAsync("/api/users/me", NewRequest(NewIdentity().Email))).StatusCode);
        }

        [Fact]
        public async Task MyProfile_WithOnlyOneUsersScope_ReturnsForbiddenForOtherOperation()
        {
            var identity = NewIdentity();
            using var writeOnly = _factory.CreateAuthenticatedClient(identity.Subject, ["users.write"]);
            using var readOnly = _factory.CreateAuthenticatedClient(NewIdentity().Subject, ["users.read"]);

            Assert.Equal(HttpStatusCode.Forbidden, (await writeOnly.GetAsync("/api/users/me")).StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden,
                (await readOnly.PutAsJsonAsync("/api/users/me", NewRequest(identity.Email))).StatusCode);
        }

        [Fact]
        public async Task UserCollectionRoutes_AreNotAvailable()
        {
            var identity = NewIdentity();
            using var client = _factory.CreateAuthenticatedClient(identity.Subject, ["users.read"]);

            Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/api/users")).StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/api/users/1")).StatusCode);
        }

        [Fact]
        public async Task GetMyProfile_DoesNotSerializeExternalId()
        {
            var identity = NewIdentity();
            using var client = _factory.CreateAuthenticatedClient(identity.Subject, ["users.read", "users.write"]);

            await client.PutAsJsonAsync("/api/users/me", NewRequest(identity.Email));
            var response = await client.GetAsync("/api/users/me");

            Assert.DoesNotContain("externalId", await response.Content.ReadAsStringAsync(), StringComparison.OrdinalIgnoreCase);
        }

        private static UserIdentity NewIdentity()
        {
            var value = Guid.NewGuid().ToString("N");
            return new UserIdentity($"subject-{value}", $"{value}@example.com");
        }

        private static UpdateUserRequest NewRequest(string email) =>
            new() { Name = "Test User", Email = email, PhoneNumber = "555-0100" };

        private sealed record UserIdentity(string Subject, string Email);
    }
}
