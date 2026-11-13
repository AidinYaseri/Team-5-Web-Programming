using System.Net;
using System.Net.Http.Json;
using Animal.API.Contracts;

namespace Animal.IntegrationTests.Api
{
    // WI-10 (#42): the user's profile follows the account across devices. Device A and Device B sign in on their
    // own (different tokens for the same sub); nothing is copied from A to B. Another user can't reach the profile.
    // The picture steps are added to this journey once the picture endpoints exist (WI-5, #37).
    public class CrossDeviceProfileTests : IClassFixture<AnimalApiFactory>
    {
        private static readonly string[] ProfileScopes = ["users.read", "users.write"];
        private readonly AnimalApiFactory _factory;

        public CrossDeviceProfileTests(AnimalApiFactory factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task Profile_SavedOnDeviceA_IsReadOnDeviceB_AndOtherUsersCantReachIt()
        {
            var user = NewUser();
            using var deviceA = _factory.SignInDevice(user.Subject, ProfileScopes, issuedAgo: TimeSpan.FromMinutes(5));
            using var deviceB = _factory.SignInDevice(user.Subject, ProfileScopes);

            // The two devices really signed in separately.
            Assert.NotEqual(deviceA.Token, deviceB.Token);
            Assert.NotEqual(deviceA.TokenId, deviceB.TokenId);

            // Device A creates the profile. Device B (separate token) sees it.
            Assert.Equal(HttpStatusCode.NoContent,
                (await deviceA.Client.PutAsJsonAsync("/api/users/me", Request("Ada on laptop", user.Email))).StatusCode);
            var seenOnB = await ReadProfileAsync(deviceB);
            Assert.Equal("Ada on laptop", seenOnB.Name);
            Assert.Equal(user.Email, seenOnB.Email);

            // Device A changes it again. Device B sees the new version, not a stale copy.
            Assert.Equal(HttpStatusCode.NoContent,
                (await deviceA.Client.PutAsJsonAsync("/api/users/me", Request("Ada updated on laptop", user.Email))).StatusCode);
            Assert.Equal("Ada updated on laptop", (await ReadProfileAsync(deviceB)).Name);

            // Device B's change shows up on Device A too: it's one account, not two copies.
            Assert.Equal(HttpStatusCode.NoContent,
                (await deviceB.Client.PutAsJsonAsync("/api/users/me", Request("Ada on phone", user.Email))).StatusCode);
            Assert.Equal("Ada on phone", (await ReadProfileAsync(deviceA)).Name);

            // Another user signs in: they only ever get their own profile, never Ada's.
            var other = NewUser();
            using var otherDevice = _factory.SignInDevice(other.Subject, ProfileScopes);
            Assert.Equal(HttpStatusCode.NotFound, (await otherDevice.Client.GetAsync("/api/users/me")).StatusCode);

            // Changing an id in the request doesn't help: there is no route that takes a user id.
            var adaId = seenOnB.Id;
            Assert.Equal(HttpStatusCode.NotFound, (await otherDevice.Client.GetAsync($"/api/users/{adaId}")).StatusCode);
            var putById = await otherDevice.Client.PutAsJsonAsync($"/api/users/{adaId}", Request("Hacked", other.Email));
            Assert.Contains(putById.StatusCode, new[] { HttpStatusCode.NotFound, HttpStatusCode.MethodNotAllowed });

            // The other user saving "their" profile creates their own, and Ada's is unchanged.
            Assert.Equal(HttpStatusCode.NoContent,
                (await otherDevice.Client.PutAsJsonAsync("/api/users/me", Request("Ben", other.Email))).StatusCode);
            var adaAfter = await ReadProfileAsync(deviceB);
            Assert.Equal("Ada on phone", adaAfter.Name);
            Assert.Equal(adaId, adaAfter.Id);
            Assert.NotEqual(adaId, (await ReadProfileAsync(otherDevice)).Id);
        }

        [Fact]
        public async Task ProfileResponse_NeverContainsATokenOrTheExternalId()
        {
            var user = NewUser();
            using var device = _factory.SignInDevice(user.Subject, ProfileScopes);
            await device.Client.PutAsJsonAsync("/api/users/me", Request("No Secrets", user.Email));

            var json = await device.Client.GetStringAsync("/api/users/me");

            Assert.DoesNotContain(device.Token, json);
            Assert.DoesNotContain(user.Subject, json);
            Assert.DoesNotContain("externalId", json, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task DeviceWithoutAToken_GetsNothing()
        {
            var user = NewUser();
            using var deviceA = _factory.SignInDevice(user.Subject, ProfileScopes);
            await deviceA.Client.PutAsJsonAsync("/api/users/me", Request("Signed In", user.Email));

            // A device that never signed in (or whose session was removed) can't read the profile.
            using var signedOut = _factory.CreateClient();
            Assert.Equal(HttpStatusCode.Unauthorized, (await signedOut.GetAsync("/api/users/me")).StatusCode);
        }

        private static async Task<UserResponse> ReadProfileAsync(AnimalApiFactory.DeviceSession device)
        {
            var response = await device.Client.GetAsync("/api/users/me");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            return (await response.Content.ReadFromJsonAsync<UserResponse>())!;
        }

        private static (string Subject, string Email) NewUser()
        {
            var value = Guid.NewGuid().ToString("N");
            return ($"device-user-{value}", $"{value}@example.test");
        }

        private static UpdateUserRequest Request(string name, string email) =>
            new() { Name = name, Email = email, PhoneNumber = "514-555-0101" };
    }
}
