using System.Net;
using System.Net.Http.Headers;
using System.Text;
using Animal.Client.Contracts;
using Animal.Client.Services;

namespace Animal.UnitTests.Client
{
    public class UsersApiClientTests
    {
        [Fact]
        public async Task GetMyProfileAsync_NotFound_ReturnsNull()
        {
            var handler = new FakeHandler(HttpStatusCode.NotFound, """{"title":"Not found."}""");
            var client = new UsersApiClient(new HttpClient(handler) { BaseAddress = new Uri("http://api.test/") });

            Assert.Null(await client.GetMyProfileAsync());
            Assert.Equal("http://api.test/api/users/me", handler.LastRequest!.RequestUri!.ToString());
        }

        [Fact]
        public async Task GetMyProfileAsync_Success_ReadsResponse()
        {
            var handler = new FakeHandler(HttpStatusCode.OK,
                """{"id":4,"name":"Sam","email":"sam@example.com","phoneNumber":"555-0100"}""");
            var client = new UsersApiClient(new HttpClient(handler) { BaseAddress = new Uri("http://api.test/") });

            var result = await client.GetMyProfileAsync();

            Assert.Equal(new UserResponse(4, "Sam", "sam@example.com", "555-0100"), result);
        }

        // The API answers 401 with an empty body, so ApiResponse uses its readable fallback message.
        // The profile page relies on this exception's status code to show "Please sign in".
        [Fact]
        public async Task GetMyProfileAsync_Unauthorized_ThrowsWithStatusCode()
        {
            var handler = new FakeHandler(HttpStatusCode.Unauthorized, "");
            var client = new UsersApiClient(new HttpClient(handler) { BaseAddress = new Uri("http://api.test/") });

            var exception = await Assert.ThrowsAsync<ApiException>(() => client.GetMyProfileAsync());

            Assert.Equal(HttpStatusCode.Unauthorized, exception.StatusCode);
            Assert.Equal(["Sign in to continue."], exception.Errors);
            Assert.Equal("Sign in to continue.", exception.Message);
        }

        [Theory]
        [InlineData(HttpStatusCode.BadRequest, "Please correct the profile.")]
        [InlineData(HttpStatusCode.Conflict, "That email is already in use.")]
        public async Task UpdateMyProfileAsync_ApiError_ThrowsWithStatusAndMessage(
            HttpStatusCode statusCode,
            string message)
        {
            var handler = new FakeHandler(statusCode, $$"""{"title":"{{message}}","status":{{(int)statusCode}}}""");
            var client = new UsersApiClient(new HttpClient(handler) { BaseAddress = new Uri("http://api.test/") });

            var exception = await Assert.ThrowsAsync<ApiException>(() => client.UpdateMyProfileAsync(
                new UpdateUserRequest { Name = "Sam", Email = "sam@example.com", PhoneNumber = "555-0100" }));

            Assert.Equal(statusCode, exception.StatusCode);
            Assert.Equal(message, exception.Message);
        }

        [Fact]
        public async Task UpdateMyProfileAsync_Success_PutsJsonBody()
        {
            var handler = new FakeHandler(HttpStatusCode.NoContent, "");
            var client = new UsersApiClient(new HttpClient(handler) { BaseAddress = new Uri("http://api.test/") });

            await client.UpdateMyProfileAsync(new UpdateUserRequest
            {
                Name = "Sam",
                Email = "sam@example.com",
                PhoneNumber = "555-0100"
            });

            Assert.Equal(HttpMethod.Put, handler.LastRequest!.Method);
            Assert.Contains("\"email\":\"sam@example.com\"", handler.LastBody);
        }

        [Fact]
        public async Task GetMyProfilePictureAsync_NoContent_ReturnsNull()
        {
            var handler = new FakeHandler(HttpStatusCode.NoContent, "");
            var client = new UsersApiClient(new HttpClient(handler) { BaseAddress = new Uri("http://api.test/") });

            var result = await client.GetMyProfilePictureAsync();

            Assert.Null(result);
            Assert.Equal(HttpMethod.Get, handler.LastRequest!.Method);
            Assert.Equal("http://api.test/api/users/me/picture", handler.LastRequest.RequestUri!.ToString());
        }

        [Fact]
        public async Task GetMyProfilePictureAsync_Success_ReturnsBytesAndContentType()
        {
            var bytes = new byte[] { 0x89, 0x50, 0x4E, 0x47 };
            var handler = new FakeHandler(HttpStatusCode.OK, bytes, "image/png");
            var client = new UsersApiClient(new HttpClient(handler) { BaseAddress = new Uri("http://api.test/") });

            var result = await client.GetMyProfilePictureAsync();

            Assert.Equal("image/png", result!.ContentType);
            Assert.Equal(bytes, result.Bytes);
        }

        [Theory]
        [InlineData(HttpStatusCode.BadRequest)]
        [InlineData(HttpStatusCode.Unauthorized)]
        [InlineData(HttpStatusCode.NotFound)]
        public async Task GetMyProfilePictureAsync_ApiError_ThrowsWithStatusCode(HttpStatusCode statusCode)
        {
            var handler = new FakeHandler(statusCode, "{\"title\":\"Picture request failed.\"}");
            var client = new UsersApiClient(new HttpClient(handler) { BaseAddress = new Uri("http://api.test/") });

            var exception = await Assert.ThrowsAsync<ApiException>(() => client.GetMyProfilePictureAsync());

            Assert.Equal(statusCode, exception.StatusCode);
            Assert.Equal("Picture request failed.", exception.Message);
        }

        [Fact]
        public async Task UploadMyProfilePictureAsync_Success_PutsMultipartFile()
        {
            var handler = new FakeHandler(HttpStatusCode.NoContent, "");
            var client = new UsersApiClient(new HttpClient(handler) { BaseAddress = new Uri("http://api.test/") });
            await using var image = new MemoryStream(Encoding.UTF8.GetBytes("image bytes"));

            await client.UploadMyProfilePictureAsync(image, "profile.png", "image/png");

            Assert.Equal(HttpMethod.Put, handler.LastRequest!.Method);
            Assert.Equal("http://api.test/api/users/me/picture", handler.LastRequest.RequestUri!.ToString());
            Assert.Equal("multipart/form-data", handler.LastRequest.Content!.Headers.ContentType!.MediaType);
            Assert.Contains("name=\"file\"; filename=\"profile.png\"", handler.LastBody);
            Assert.Contains("Content-Type: image/png", handler.LastBody);
        }

        [Theory]
        [InlineData(HttpStatusCode.BadRequest)]
        [InlineData(HttpStatusCode.Unauthorized)]
        [InlineData(HttpStatusCode.NotFound)]
        public async Task UploadMyProfilePictureAsync_ApiError_ThrowsWithStatusCode(HttpStatusCode statusCode)
        {
            var handler = new FakeHandler(statusCode, "{\"title\":\"Picture upload failed.\"}");
            var client = new UsersApiClient(new HttpClient(handler) { BaseAddress = new Uri("http://api.test/") });
            await using var image = new MemoryStream([1, 2, 3]);

            var exception = await Assert.ThrowsAsync<ApiException>(() =>
                client.UploadMyProfilePictureAsync(image, "profile.jpg", "image/jpeg"));

            Assert.Equal(statusCode, exception.StatusCode);
            Assert.Equal("Picture upload failed.", exception.Message);
        }

        [Fact]
        public async Task DeleteMyProfilePictureAsync_Success_DeletesPicture()
        {
            var handler = new FakeHandler(HttpStatusCode.NoContent, "");
            var client = new UsersApiClient(new HttpClient(handler) { BaseAddress = new Uri("http://api.test/") });

            await client.DeleteMyProfilePictureAsync();

            Assert.Equal(HttpMethod.Delete, handler.LastRequest!.Method);
            Assert.Equal("http://api.test/api/users/me/picture", handler.LastRequest.RequestUri!.ToString());
        }

        [Theory]
        [InlineData(HttpStatusCode.BadRequest)]
        [InlineData(HttpStatusCode.Unauthorized)]
        [InlineData(HttpStatusCode.NotFound)]
        public async Task DeleteMyProfilePictureAsync_ApiError_ThrowsWithStatusCode(HttpStatusCode statusCode)
        {
            var handler = new FakeHandler(statusCode, "{\"title\":\"Picture removal failed.\"}");
            var client = new UsersApiClient(new HttpClient(handler) { BaseAddress = new Uri("http://api.test/") });

            var exception = await Assert.ThrowsAsync<ApiException>(() => client.DeleteMyProfilePictureAsync());

            Assert.Equal(statusCode, exception.StatusCode);
            Assert.Equal("Picture removal failed.", exception.Message);
        }

        private sealed class FakeHandler : HttpMessageHandler
        {
            private readonly HttpStatusCode _statusCode;
            private readonly byte[] _body;
            private readonly string _mediaType;

            public FakeHandler(HttpStatusCode statusCode, string body)
                : this(statusCode, Encoding.UTF8.GetBytes(body), "application/json")
            {
            }

            public FakeHandler(HttpStatusCode statusCode, byte[] body, string mediaType)
            {
                _statusCode = statusCode;
                _body = body;
                _mediaType = mediaType;
            }

            public HttpRequestMessage? LastRequest { get; private set; }
            public string? LastBody { get; private set; }

            protected override async Task<HttpResponseMessage> SendAsync(
                HttpRequestMessage request,
                CancellationToken cancellationToken)
            {
                LastRequest = request;
                LastBody = request.Content is null
                    ? null
                    : await request.Content.ReadAsStringAsync(cancellationToken);

                return new HttpResponseMessage(_statusCode)
                {
                    Content = new ByteArrayContent(_body)
                    {
                        Headers = { ContentType = new MediaTypeHeaderValue(_mediaType) }
                    }
                };
            }
        }
    }
}
