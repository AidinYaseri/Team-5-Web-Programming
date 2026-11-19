using System.Net;
using System.Net.Http.Json;
using System.Net.Http.Headers;
using Animal.API.Contracts;

namespace Animal.IntegrationTests.Api
{
    public class ProfilePictureEndpointTests : IClassFixture<AnimalApiFactory>
    {
        private static readonly byte[] PngBytes =
        [
            0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A,
            0x00, 0x00, 0x00, 0x00
        ];

        private readonly AnimalApiFactory _factory;

        public ProfilePictureEndpointTests(AnimalApiFactory factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task PictureEndpoints_WithoutToken_ReturnUnauthorized()
        {
            using var client = _factory.CreateClient();

            Assert.Equal(HttpStatusCode.Unauthorized,
                (await client.GetAsync("/api/users/me/picture")).StatusCode);
            Assert.Equal(HttpStatusCode.Unauthorized,
                (await client.PutAsync("/api/users/me/picture", CreateUpload())).StatusCode);
            Assert.Equal(HttpStatusCode.Unauthorized,
                (await client.DeleteAsync("/api/users/me/picture")).StatusCode);
        }

        [Fact]
        public async Task PictureEndpoints_WithInsufficientScope_ReturnForbidden()
        {
            using var readOnly = _factory.CreateAuthenticatedClient(
                $"picture-read-{Guid.NewGuid():N}", ["users.read"]);
            using var writeOnly = _factory.CreateAuthenticatedClient(
                $"picture-write-{Guid.NewGuid():N}", ["users.write"]);

            Assert.Equal(HttpStatusCode.Forbidden,
                (await readOnly.PutAsync("/api/users/me/picture", CreateUpload())).StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden,
                (await writeOnly.GetAsync("/api/users/me/picture")).StatusCode);
        }

        [Fact]
        public async Task PictureEndpoints_WithoutProfile_ReturnNotFound()
        {
            using var client = _factory.CreateAuthenticatedClient(
                $"picture-no-profile-{Guid.NewGuid():N}", ["users.read", "users.write"]);

            Assert.Equal(HttpStatusCode.NotFound,
                (await client.GetAsync("/api/users/me/picture")).StatusCode);
            Assert.Equal(HttpStatusCode.NotFound,
                (await client.PutAsync("/api/users/me/picture", CreateUpload())).StatusCode);
            Assert.Equal(HttpStatusCode.NotFound,
                (await client.DeleteAsync("/api/users/me/picture")).StatusCode);
        }

        [Fact]
        public async Task PictureEndpoints_UploadGetDelete_PersistPrivatePicture()
        {
            var identity = NewIdentity();
            using var client = _factory.CreateAuthenticatedClient(
                identity.Subject, ["users.read", "users.write"]);
            Assert.Equal(HttpStatusCode.NoContent,
                (await client.PutAsJsonAsync("/api/users/me", new UpdateUserRequest
                {
                    Name = "Picture User",
                    Email = identity.Email,
                    PhoneNumber = "555-0100"
                })).StatusCode);

            Assert.Equal(HttpStatusCode.NoContent,
                (await client.GetAsync("/api/users/me/picture")).StatusCode);
            Assert.Equal(HttpStatusCode.NoContent,
                (await client.PutAsync("/api/users/me/picture", CreateUpload())).StatusCode);

            using var response = await client.GetAsync("/api/users/me/picture");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("image/png", response.Content.Headers.ContentType?.MediaType);
            Assert.Equal(PngBytes, await response.Content.ReadAsByteArrayAsync());
            Assert.True(response.Headers.CacheControl?.Private);
            Assert.True(response.Headers.CacheControl?.NoStore);

            Assert.Equal(HttpStatusCode.NoContent,
                (await client.DeleteAsync("/api/users/me/picture")).StatusCode);
            Assert.Equal(HttpStatusCode.NoContent,
                (await client.GetAsync("/api/users/me/picture")).StatusCode);
        }

        [Fact]
        public async Task UploadMyProfilePicture_InvalidFormat_ReturnsBadRequest()
        {
            var identity = NewIdentity();
            using var client = _factory.CreateAuthenticatedClient(identity.Subject, ["users.read", "users.write"]);
            await client.PutAsJsonAsync("/api/users/me", new UpdateUserRequest
            {
                Name = "Picture User",
                Email = identity.Email,
                PhoneNumber = "555-0100"
            });
            Assert.Equal(HttpStatusCode.NoContent,
                (await client.PutAsync("/api/users/me/picture", CreateUpload())).StatusCode);

            using var content = new MultipartFormDataContent();
            using var invalid = new ByteArrayContent([1, 2, 3, 4]);
            invalid.Headers.ContentType = new MediaTypeHeaderValue("image/png");
            content.Add(invalid, "file", "not-a-png.png");

            using var response = await client.PutAsync("/api/users/me/picture", content);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            using var savedPicture = await client.GetAsync("/api/users/me/picture");
            Assert.Equal(PngBytes, await savedPicture.Content.ReadAsByteArrayAsync());
        }

        private static MultipartFormDataContent CreateUpload()
        {
            var content = new MultipartFormDataContent();
            var image = new ByteArrayContent(PngBytes);
            image.Headers.ContentType = new MediaTypeHeaderValue("image/png");
            content.Add(image, "file", "profile.png");
            return content;
        }

        private static UserIdentity NewIdentity()
        {
            var value = Guid.NewGuid().ToString("N");
            return new UserIdentity($"picture-sub-{value}", $"{value}@example.com");
        }

        private sealed record UserIdentity(string Subject, string Email);
    }
}
