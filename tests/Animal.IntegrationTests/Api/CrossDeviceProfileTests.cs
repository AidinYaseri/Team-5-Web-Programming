using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Animal.API.Contracts;

namespace Animal.IntegrationTests.Api
{
    // WI-10 (#42): the user's profile follows the account across devices. Device A and Device B sign in on their
    // own (different tokens for the same sub); nothing is copied from A to B. Another user can't reach the profile.
    // The picture journey uses the endpoints from WI-5 (#53): the picture lives in shared storage, not in a session.
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
        public async Task Picture_UploadedOnDeviceA_IsServedToDeviceB_AndOtherUsersCantReadOrReplaceIt()
        {
            var user = NewUser();
            using var deviceA = _factory.SignInDevice(user.Subject, ProfileScopes, issuedAgo: TimeSpan.FromMinutes(5));
            using var deviceB = _factory.SignInDevice(user.Subject, ProfileScopes);
            Assert.NotEqual(deviceA.Token, deviceB.Token);
            await deviceA.Client.PutAsJsonAsync("/api/users/me", Request("Ada", user.Email));

            // Device B (signed in separately) sees there is no picture yet.
            Assert.False((await ReadProfileAsync(deviceB)).HasPicture);
            Assert.Equal(HttpStatusCode.NotFound, (await deviceB.Client.GetAsync("/api/users/me/picture")).StatusCode);

            // Device A uploads a PNG. Device B gets exactly the picture A's account now has, from shared storage.
            Assert.Equal(HttpStatusCode.NoContent, (await UploadAsync(deviceA, "profile-picture.png", "image/png")).StatusCode);
            var onA = await ReadPictureAsync(deviceA);
            var onB = await ReadPictureAsync(deviceB);
            Assert.Equal("image/png", onB.ContentType);
            Assert.Equal(onA.Bytes, onB.Bytes);
            Assert.True((await ReadProfileAsync(deviceB)).HasPicture);

            // Device A replaces it with a JPEG. Device B gets the new one, not a stale copy.
            Assert.Equal(HttpStatusCode.NoContent, (await UploadAsync(deviceA, "profile-picture.jpg", "image/jpeg")).StatusCode);
            var replacedOnB = await ReadPictureAsync(deviceB);
            Assert.Equal("image/jpeg", replacedOnB.ContentType);
            Assert.NotEqual(onB.Bytes, replacedOnB.Bytes);

            // Another user, with their own profile, can't read Ada's picture: "me" is always themselves.
            var other = NewUser();
            using var otherDevice = _factory.SignInDevice(other.Subject, ProfileScopes);
            await otherDevice.Client.PutAsJsonAsync("/api/users/me", Request("Ben", other.Email));
            Assert.Equal(HttpStatusCode.NotFound, (await otherDevice.Client.GetAsync("/api/users/me/picture")).StatusCode);

            // Changing an id in the request doesn't reach it either.
            var adaId = (await ReadProfileAsync(deviceB)).Id;
            Assert.Equal(HttpStatusCode.NotFound, (await otherDevice.Client.GetAsync($"/api/users/{adaId}/picture")).StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, (await otherDevice.Client.GetAsync($"/api/users/me/picture?userId={adaId}")).StatusCode);

            // The other user uploading or deleting "their" picture never touches Ada's.
            Assert.Equal(HttpStatusCode.NoContent, (await UploadAsync(otherDevice, "profile-picture.png", "image/png")).StatusCode);
            Assert.Equal(HttpStatusCode.NoContent, (await otherDevice.Client.DeleteAsync("/api/users/me/picture")).StatusCode);
            var stillAdas = await ReadPictureAsync(deviceB);
            Assert.Equal("image/jpeg", stillAdas.ContentType);
            Assert.Equal(replacedOnB.Bytes, stillAdas.Bytes);

            // Device A deletes the picture. Device B sees it's gone.
            Assert.Equal(HttpStatusCode.NoContent, (await deviceA.Client.DeleteAsync("/api/users/me/picture")).StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, (await deviceB.Client.GetAsync("/api/users/me/picture")).StatusCode);
            Assert.False((await ReadProfileAsync(deviceB)).HasPicture);
        }

        [Fact]
        public async Task Picture_RejectedUploadOnDeviceA_LeavesDeviceBsViewUnchanged()
        {
            var user = NewUser();
            using var deviceA = _factory.SignInDevice(user.Subject, ProfileScopes);
            using var deviceB = _factory.SignInDevice(user.Subject, ProfileScopes);
            await deviceA.Client.PutAsJsonAsync("/api/users/me", Request("Ada", user.Email));
            await UploadAsync(deviceA, "profile-picture.png", "image/png");
            var before = await ReadPictureAsync(deviceB);

            // A GIF and a JPEG pretending to be a PNG are refused...
            Assert.Equal(HttpStatusCode.BadRequest, (await UploadAsync(deviceA, "profile-picture.gif", "image/gif")).StatusCode);
            Assert.Equal(HttpStatusCode.BadRequest, (await UploadAsync(deviceA, "profile-picture.jpg", "image/png")).StatusCode);

            // ...and Device B still gets the old picture.
            var after = await ReadPictureAsync(deviceB);
            Assert.Equal(before.ContentType, after.ContentType);
            Assert.Equal(before.Bytes, after.Bytes);
        }

        [Fact]
        public async Task PictureResponse_IsPrivateAndCarriesNoToken()
        {
            var user = NewUser();
            using var device = _factory.SignInDevice(user.Subject, ProfileScopes);
            await device.Client.PutAsJsonAsync("/api/users/me", Request("Ada", user.Email));
            await UploadAsync(device, "profile-picture.png", "image/png");

            var response = await device.Client.GetAsync("/api/users/me/picture");

            Assert.True(response.Headers.CacheControl?.Private);
            Assert.True(response.Headers.CacheControl?.NoStore);
            Assert.DoesNotContain(response.Headers.Concat(response.Content.Headers),
                header => header.Value.Any(value => value.Contains(device.Token, StringComparison.Ordinal)));
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

        private static async Task<HttpResponseMessage> UploadAsync(AnimalApiFactory.DeviceSession device, string testFile, string contentType)
        {
            var bytes = await File.ReadAllBytesAsync(Path.Combine(AppContext.BaseDirectory, "TestData", testFile));
            using var content = new MultipartFormDataContent();
            var file = new ByteArrayContent(bytes);
            file.Headers.ContentType = MediaTypeHeaderValue.Parse(contentType);
            content.Add(file, "file", testFile);
            return await device.Client.PutAsync("/api/users/me/picture", content);
        }

        private static async Task<(string? ContentType, byte[] Bytes)> ReadPictureAsync(AnimalApiFactory.DeviceSession device)
        {
            var response = await device.Client.GetAsync("/api/users/me/picture");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            return (response.Content.Headers.ContentType?.MediaType, await response.Content.ReadAsByteArrayAsync());
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
