using System.Net;
using System.Text;
using Animal.Client.Contracts;
using Animal.Client.Services;

namespace Animal.UnitTests.Client
{
    // Checks the client's HTTP calls with a fake handler, so no API has to be running.
    public class PetsApiClientTests
    {
        private readonly FakeHandler _handler = new();
        private readonly PetsApiClient _client;

        public PetsApiClientTests()
        {
            _client = new PetsApiClient(new HttpClient(_handler) { BaseAddress = new Uri("http://api.test/") });
        }

        [Fact]
        public async Task GetPetsAsync_NoFilters_CallsPlainUrl()
        {
            _handler.Respond(HttpStatusCode.OK, "[]");

            await _client.GetPetsAsync(null, null, false);

            Assert.Equal("http://api.test/api/pets", _handler.LastRequest!.RequestUri!.ToString());
        }

        [Fact]
        public async Task GetPetsAsync_WithFilters_BuildsQueryString()
        {
            _handler.Respond(HttpStatusCode.OK, "[]");

            await _client.GetPetsAsync("  guinea pig ", 5, true);

            Assert.Equal("?species=guinea%20pig&maxAge=5&availableOnly=true", _handler.LastRequest!.RequestUri!.Query);
        }

        [Fact]
        public async Task GetPetsAsync_ReadsCamelCaseJson()
        {
            _handler.Respond(HttpStatusCode.OK,
                """[{"id":1,"name":"Biscuit","species":"Dog","breed":"Beagle","age":3,"isAvailable":true,"shelterId":1}]""");

            var pets = await _client.GetPetsAsync(null, null, false);

            Assert.Equal([new PetResponse(1, "Biscuit", "Dog", "Beagle", 3, true, 1)], pets);
        }

        [Fact]
        public async Task GetPetAsync_NotFound_ReturnsNull()
        {
            _handler.Respond(HttpStatusCode.NotFound, """{"title":"Not found."}""");

            Assert.Null(await _client.GetPetAsync(999));
        }

        [Fact]
        public async Task CreatePetAsync_PostsJsonBody()
        {
            _handler.Respond(HttpStatusCode.Created,
                """{"id":6,"name":"Nugget","species":"Rabbit","breed":"","age":1,"isAvailable":true,"shelterId":2}""");

            var created = await _client.CreatePetAsync(new PetRequest { Name = "Nugget", Species = "Rabbit", Age = 1, ShelterId = 2 });

            Assert.Equal(HttpMethod.Post, _handler.LastRequest!.Method);
            Assert.Contains("\"name\":\"Nugget\"", _handler.LastBody);
            Assert.Equal(6, created.Id);
        }

        [Fact]
        public async Task CreatePetAsync_DomainError_ThrowsWithTheMessage()
        {
            _handler.Respond(HttpStatusCode.BadRequest,
                """{"title":"One or more business rules were broken.","status":400,"errors":["Shelter 99 does not exist."]}""");

            var ex = await Assert.ThrowsAsync<ApiException>(() => _client.CreatePetAsync(new PetRequest()));

            Assert.Equal(HttpStatusCode.BadRequest, ex.StatusCode);
            Assert.Equal(["Shelter 99 does not exist."], ex.Errors);
        }

        [Fact]
        public async Task UpdatePetAsync_ModelValidationError_FlattensFieldErrors()
        {
            _handler.Respond(HttpStatusCode.BadRequest,
                """{"title":"One or more validation errors occurred.","errors":{"Name":["The Name field is required."],"Age":["Too old."]}}""");

            var ex = await Assert.ThrowsAsync<ApiException>(() => _client.UpdatePetAsync(1, new PetRequest()));

            Assert.Equal(["The Name field is required.", "Too old."], ex.Errors);
        }

        [Fact]
        public async Task DeletePetAsync_Conflict_UsesTitleWhenThereAreNoErrors()
        {
            _handler.Respond(HttpStatusCode.Conflict, """{"title":"The change conflicts with existing data.","status":409}""");

            var ex = await Assert.ThrowsAsync<ApiException>(() => _client.DeletePetAsync(1));

            Assert.Equal(HttpStatusCode.Conflict, ex.StatusCode);
            Assert.Equal("The change conflicts with existing data.", ex.Message);
        }

        [Fact]
        public async Task ErrorWithoutJsonBody_FallsBackToStatusCode()
        {
            _handler.Respond(HttpStatusCode.BadGateway, "<html>Bad gateway</html>");

            var ex = await Assert.ThrowsAsync<ApiException>(() => _client.DeletePetAsync(1));

            Assert.Empty(ex.Errors);
            Assert.Equal("The API returned 502.", ex.Message);
        }

        private sealed class FakeHandler : HttpMessageHandler
        {
            private HttpStatusCode _status = HttpStatusCode.OK;
            private string _body = "";

            public HttpRequestMessage? LastRequest { get; private set; }
            public string? LastBody { get; private set; }

            public void Respond(HttpStatusCode status, string body)
            {
                _status = status;
                _body = body;
            }

            protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                LastRequest = request;
                LastBody = request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken);

                return new HttpResponseMessage(_status)
                {
                    Content = new StringContent(_body, Encoding.UTF8, "application/json")
                };
            }
        }
    }
}
