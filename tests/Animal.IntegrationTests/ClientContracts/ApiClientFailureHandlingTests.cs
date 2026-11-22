extern alias ClientApp;

using System.Net;
using System.Text;
using ClientServices = ClientApp::Animal.Client.Services;

namespace Animal.IntegrationTests.ClientContracts
{
    public sealed class ApiClientFailureHandlingTests
    {
        private static readonly (string Name, Func<HttpClient, Task> Invoke)[] ClientCalls =
        [
            ("pets", async http => { await new ClientServices.PetsApiClient(http).GetPetsAsync(null, null, false); }),
            ("shelters", async http => { await new ClientServices.SheltersApiClient(http).GetSheltersAsync(); }),
            ("users", async http => { await new ClientServices.UsersApiClient(http).GetMyProfileAsync(); }),
            ("adoptions", async http => { await new ClientServices.AdoptionsApiClient(http).GetMineAsync(); }),
            ("appointments", async http => { await new ClientServices.AppointmentsApiClient(http).GetMineAsync(); }),
            ("notifications", async http => { await new ClientServices.NotificationsApiClient(http).GetAsync(); })
        ];

        [Fact]
        public async Task EveryClient_500And503_ThrowsReadableApiException()
        {
            foreach (var (name, invoke) in ClientCalls)
            {
                foreach (var status in new[] { HttpStatusCode.InternalServerError, HttpStatusCode.ServiceUnavailable })
                {
                    using var http = CreateClient(new ResponseHandler(
                        status, "{\"detail\":\"The test dependency failed.\"}"));
                    var error = await Assert.ThrowsAsync<ClientServices.ApiException>(() => invoke(http));

                    Assert.Equal(status, error.StatusCode);
                    Assert.Contains("The test dependency failed.", error.Message);
                }
            }
        }

        [Fact]
        public async Task EveryClient_EmptyOrInvalidErrorBody_UsesReadableStatusFallback()
        {
            foreach (var (name, invoke) in ClientCalls)
            {
                foreach (var (status, body) in new[]
                {
                    (HttpStatusCode.ServiceUnavailable, string.Empty),
                    (HttpStatusCode.InternalServerError, "this is not JSON")
                })
                {
                    using var http = CreateClient(new ResponseHandler(status, body));
                    var error = await Assert.ThrowsAsync<ClientServices.ApiException>(() => invoke(http));

                    Assert.Equal(status, error.StatusCode);
                    Assert.False(string.IsNullOrWhiteSpace(error.Message), $"{name} returned an empty error message.");
                    var expectedMessage = status == HttpStatusCode.InternalServerError
                        ? "The service had a problem."
                        : ((int)status).ToString();
                    Assert.Contains(expectedMessage, error.Message);
                }
            }
        }

        [Fact]
        public async Task EveryClient_Timeout_PropagatesClearTimeoutError()
        {
            foreach (var (name, invoke) in ClientCalls)
            {
                using var http = CreateClient(new ResponseHandler(
                    (_, _) => Task.FromException<HttpResponseMessage>(
                        new TaskCanceledException("The request timed out."))));
                var error = await Assert.ThrowsAsync<TaskCanceledException>(() => invoke(http));

                Assert.Contains("timed out", error.Message, StringComparison.OrdinalIgnoreCase);
            }
        }

        private static HttpClient CreateClient(HttpMessageHandler handler) =>
            new(handler) { BaseAddress = new Uri("http://contract-test/") };

        private sealed class ResponseHandler : HttpMessageHandler
        {
            private readonly Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> _send;

            public ResponseHandler(HttpStatusCode statusCode, string body)
                : this((_, _) => Task.FromResult(new HttpResponseMessage(statusCode)
                {
                    Content = new StringContent(body, Encoding.UTF8, "application/json")
                }))
            {
            }

            public ResponseHandler(
                Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> send) =>
                _send = send;

            protected override Task<HttpResponseMessage> SendAsync(
                HttpRequestMessage request, CancellationToken cancellationToken) =>
                _send(request, cancellationToken);
        }
    }
}