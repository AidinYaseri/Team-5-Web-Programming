extern alias ClientApp;

using ClientModels = ClientApp::Animal.Client.Contracts;
using ClientServices = ClientApp::Animal.Client.Services;
using Animal.IntegrationTests.Api;

namespace Animal.IntegrationTests.ClientContracts
{
    public sealed class UsersApiClientContractTests : IClassFixture<AnimalApiFactory>
    {
        private readonly AnimalApiFactory _factory;
        public UsersApiClientContractTests(AnimalApiFactory factory) => _factory = factory;

        [Fact]
        public async Task ProfileRequest_IsAcceptedAndEveryClientResponseFieldMatches()
        {
            var suffix = Guid.NewGuid().ToString("N");
            using var http = _factory.CreateAuthenticatedClient(
                $"user-contract-{suffix}", ["users.read", "users.write"]);
            var users = new ClientServices.UsersApiClient(http);
            var request = new ClientModels.UpdateUserRequest
            {
                Name = "Contract User", Email = $"{suffix}@example.test", PhoneNumber = "555-0187"
            };

            await users.UpdateMyProfileAsync(request);
            var profile = await users.GetMyProfileAsync();

            Assert.NotNull(profile);
            Assert.True(profile.Id > 0);
            Assert.Equal(request.Name, profile.Name);
            Assert.Equal(request.Email, profile.Email);
            Assert.Equal(request.PhoneNumber, profile.PhoneNumber);
        }

        [Fact]
        public async Task MissingProfile_IsReturnedAsNull()
        {
            using var http = _factory.CreateAuthenticatedClient(
                $"user-without-profile-{Guid.NewGuid():N}", ["users.read"]);
            var users = new ClientServices.UsersApiClient(http);
            Assert.Null(await users.GetMyProfileAsync());
        }
    }
}