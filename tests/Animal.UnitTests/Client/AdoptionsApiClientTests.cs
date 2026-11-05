using System.Net;
using System.Text;
using Animal.Client.Services;

namespace Animal.UnitTests.Client
{
    public class AdoptionsApiClientTests
    {
        [Fact]
        public async Task ApplyAsync_PostsPetIdOnly()
        {
            var handler = new FakeHandler(HttpStatusCode.Created,
                """{"id":7,"petId":1,"petName":"Biscuit","userId":51,"status":"Pending","applicationDate":"2026-11-05T12:00:00Z"}""");
            var client = new AdoptionsApiClient(new HttpClient(handler) { BaseAddress = new Uri("http://api.test/") });

            var result = await client.ApplyAsync(1);

            Assert.Equal(HttpMethod.Post, handler.LastRequest!.Method);
            Assert.Equal("http://api.test/api/adoptions", handler.LastRequest.RequestUri!.ToString());
            Assert.Equal("""{"petId":1}""", handler.LastBody);
            Assert.Equal("Pending", result.Status);
        }

        [Fact]
        public async Task GetQueueAsync_PassesStatusFilter()
        {
            var handler = new FakeHandler(HttpStatusCode.OK, "[]");
            var client = new AdoptionsApiClient(new HttpClient(handler) { BaseAddress = new Uri("http://api.test/") });

            await client.GetQueueAsync("Pending");

            Assert.Equal("http://api.test/api/adoptions?status=Pending", handler.LastRequest!.RequestUri!.ToString());
        }

        [Fact]
        public async Task ApplyAsync_Conflict_ThrowsWithApiMessage()
        {
            var handler = new FakeHandler(HttpStatusCode.Conflict,
                """{"title":"The request conflicts with existing data.","status":409,"detail":"You already have a pending application for this pet."}""");
            var client = new AdoptionsApiClient(new HttpClient(handler) { BaseAddress = new Uri("http://api.test/") });

            var ex = await Assert.ThrowsAsync<ApiException>(() => client.ApplyAsync(1));

            Assert.Equal(HttpStatusCode.Conflict, ex.StatusCode);
            Assert.Equal("You already have a pending application for this pet.", ex.Message);
        }

        private sealed class FakeHandler(HttpStatusCode status, string body) : HttpMessageHandler
        {
            public HttpRequestMessage? LastRequest { get; private set; }
            public string? LastBody { get; private set; }

            protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                LastRequest = request;
                LastBody = request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken);
                return new HttpResponseMessage(status) { Content = new StringContent(body, Encoding.UTF8, "application/json") };
            }
        }
    }
}
