extern alias ClientApp;

using ClientModels = ClientApp::Animal.Client.Contracts;
using ClientServices = ClientApp::Animal.Client.Services;
using Animal.IntegrationTests.Api;
using Animal.Domain.Entities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Animal.IntegrationTests.ClientContracts
{
    public sealed class AppointmentsApiClientContractTests
    {
        private static readonly DateTimeOffset FixedNow = new(2036, 3, 4, 12, 0, 0, TimeSpan.Zero);

        [Fact]
        public async Task AppointmentRequest_IsAcceptedAndEveryResponseFieldMatches()
        {
            Action<IServiceCollection> configure = services =>
            {
                services.RemoveAll<TimeProvider>();
                services.AddSingleton<TimeProvider>(new FixedTimeProvider(FixedNow));
            };
            using var factory = new AnimalApiFactory(configure);
            const string subject = "appointment-contract-user";
            factory.ResetUserData(
            [
                new User
                {
                    Id = 41, ExternalId = subject, Name = "Contract Visitor",
                    Email = "appointment-contract@example.test", PhoneNumber = "555-0185"
                }
            ]);

            using var http = factory.CreateAuthenticatedClient(
                subject, ["appointments.read", "appointments.write"]);
            var appointments = new ClientServices.AppointmentsApiClient(http);
            var requestedDate = FixedNow.AddDays(14).UtcDateTime;
            var request = new ClientModels.CreateAppointmentRequest
            {
                PetId = 1, AppointmentDate = requestedDate
            };

            var created = await appointments.CreateAsync(request);
            Assert.True(created.Id > 0);
            Assert.Equal(1, created.PetId);
            Assert.Equal(1, created.ShelterId);
            Assert.Equal(requestedDate, created.AppointmentDate);
            Assert.Equal("Scheduled", created.Status);

            var mine = await appointments.GetMineAsync();
            Assert.Equal(created, Assert.Single(mine));
        }

        private sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
        {
            public override DateTimeOffset GetUtcNow() => utcNow;
        }
    }
}