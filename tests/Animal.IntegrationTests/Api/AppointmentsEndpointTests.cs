using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Animal.Data.Context;
using Animal.Domain.Entities;
using Microsoft.Extensions.DependencyInjection;

namespace Animal.IntegrationTests.Api
{
    public sealed class AppointmentsEndpointTests : IClassFixture<AnimalApiFactory>
    {
        private static readonly DateTime FutureSlot = new(2099, 1, 1, 10, 0, 0, DateTimeKind.Utc);
        private readonly AnimalApiFactory _factory;
        private readonly HttpClient _client;

        public AppointmentsEndpointTests(AnimalApiFactory factory)
        {
            _factory = factory;
            factory.ResetUserData([User(41), User(42)]);
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task GetMine_WithoutToken_ReturnsUnauthorized()
        {
            var response = await _client.GetAsync("/api/appointments");

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task GetMine_WithoutReadScope_ReturnsForbidden()
        {
            UseTestToken(41, "notifications.read");

            var response = await _client.GetAsync("/api/appointments");

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task GetMine_ReturnsOnlyCurrentUsersAppointments()
        {
            _factory.ResetUserData([User(41), User(42)], appointments:
            [
                new Appointment { Id = 201, UserId = 41, PetId = 1, ShelterId = 1, AppointmentDate = FutureSlot },
                new Appointment { Id = 202, UserId = 42, PetId = 2, ShelterId = 1, AppointmentDate = FutureSlot.AddDays(1) }
            ]);
            UseTestToken(41, "appointments.read");

            var result = await _client.GetFromJsonAsync<List<AppointmentResponse>>("/api/appointments");

            Assert.NotNull(result);
            Assert.Equal([201], result.Select(appointment => appointment.Id));
        }

        [Fact]
        public async Task Create_PastDate_ReturnsConflict()
        {
            UseTestToken(41, "appointments.write");

            var response = await _client.PostAsJsonAsync("/api/appointments", new
            {
                petId = 1,
                appointmentDate = new DateTime(2000, 1, 1, 10, 0, 0, DateTimeKind.Utc)
            });

            Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        }

        [Fact]
        public async Task Create_TakenShelterSlot_ReturnsConflict()
        {
            UseTestToken(41, "appointments.write");
            var first = await BookAsync(1);
            UseTestToken(42, "appointments.write");

            var second = await BookAsync(2);

            Assert.Equal(HttpStatusCode.Created, first.StatusCode);
            Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
        }

        [Fact]
        public async Task Create_ReturnsStringStatusAndOwnerCanCancelOnlyOnce()
        {
            UseTestToken(41, "appointments.write");
            var create = await BookAsync(1);
            var appointment = await create.Content.ReadFromJsonAsync<AppointmentResponse>();
            Assert.Equal(HttpStatusCode.Created, create.StatusCode);
            Assert.NotNull(appointment);
            Assert.Equal("Scheduled", appointment.Status);

            UseTestToken(42, "appointments.write");
            var hiddenFromOtherOwner = await _client.PostAsync($"/api/appointments/{appointment.Id}/cancel", content: null);
            Assert.Equal(HttpStatusCode.NotFound, hiddenFromOtherOwner.StatusCode);

            UseTestToken(41, "appointments.write");
            var cancel = await _client.PostAsync($"/api/appointments/{appointment.Id}/cancel", content: null);
            var cancelAgain = await _client.PostAsync($"/api/appointments/{appointment.Id}/cancel", content: null);
            Assert.Equal(HttpStatusCode.NoContent, cancel.StatusCode);
            Assert.Equal(HttpStatusCode.Conflict, cancelAgain.StatusCode);

            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AnimalDbContext>();
            Assert.Equal(2, context.Notifications.Count(notification => notification.UserId == 41));
        }

        private Task<HttpResponseMessage> BookAsync(int petId) => _client.PostAsJsonAsync("/api/appointments", new
        {
            petId,
            appointmentDate = FutureSlot
        });

        private void UseTestToken(int userId, string scope) =>
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(TestAuthenticationHandler.SchemeName, $"{userId} {scope}");

        private static User User(int id) => new()
        {
            Id = id,
            Name = $"Test User {id}",
            Email = $"user{id}@example.test",
            PhoneNumber = "555-0100"
        };

        private sealed record AppointmentResponse(int Id, int PetId, int ShelterId, DateTime AppointmentDate, string Status);
    }
}
