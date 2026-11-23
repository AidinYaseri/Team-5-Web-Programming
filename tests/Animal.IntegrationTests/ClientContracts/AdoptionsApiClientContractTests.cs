extern alias ClientApp;

using ClientModels = ClientApp::Animal.Client.Contracts;
using ClientServices = ClientApp::Animal.Client.Services;
using Animal.IntegrationTests.Api;
using Animal.Domain.Entities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Animal.IntegrationTests.ClientContracts
{
    public sealed class AdoptionsApiClientContractTests
    {
        private static readonly DateTimeOffset FixedNow = new(2035, 2, 3, 12, 0, 0, TimeSpan.Zero);

        [Fact]
        public async Task ApplyRequest_IsAcceptedAndEveryResponseFieldMatches()
        {
            Action<IServiceCollection> configure = services =>
            {
                services.RemoveAll<TimeProvider>();
                services.AddSingleton<TimeProvider>(new FixedTimeProvider(FixedNow));
            };
            using var factory = new AnimalApiFactory(configure);
            const string subject = "adoption-contract-user";
            factory.ResetUserData(
            [
                new User
                {
                    Id = 51, ExternalId = subject, Name = "Contract Adopter",
                    Email = "adoption-contract@example.test", PhoneNumber = "555-0186"
                }
            ]);

            using var http = factory.CreateAuthenticatedClient(subject, ["adoptions.read", "adoptions.write"]);
            var adoptions = new ClientServices.AdoptionsApiClient(http);

            var created = await adoptions.ApplyAsync(1);
            Assert.True(created.Id > 0);
            Assert.Equal(1, created.PetId);
            Assert.Equal("Biscuit", created.PetName);
            Assert.Equal(51, created.UserId);
            Assert.Equal("Pending", created.Status);
            Assert.Equal(FixedNow.UtcDateTime, created.ApplicationDate);

            var mine = await adoptions.GetMineAsync();
            Assert.Equal(created, Assert.Single(mine));
        }

        private sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
        {
            public override DateTimeOffset GetUtcNow() => utcNow;
        }
    }
}