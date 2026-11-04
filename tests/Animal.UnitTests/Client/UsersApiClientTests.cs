using System.Net;
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

        private sealed class FakeHandler(HttpStatusCode statusCode, string body) : HttpMessageHandler
        {
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

                return new HttpResponseMessage(statusCode)
                {
                    Content = new StringContent(body, Encoding.UTF8, "application/json")
                };
            }
        }
    }
}
