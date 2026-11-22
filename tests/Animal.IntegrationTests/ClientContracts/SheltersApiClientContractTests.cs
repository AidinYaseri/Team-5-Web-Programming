extern alias ClientApp;

using ClientModels = ClientApp::Animal.Client.Contracts;
using ClientServices = ClientApp::Animal.Client.Services;
using Animal.IntegrationTests.Api;

namespace Animal.IntegrationTests.ClientContracts
{
    public sealed class SheltersApiClientContractTests : IClassFixture<AnimalApiFactory>
    {
        private readonly AnimalApiFactory _factory;
        public SheltersApiClientContractTests(AnimalApiFactory factory) => _factory = factory;

        [Fact]
        public async Task ShelterRequests_AreAcceptedAndEveryResponseFieldRoundTrips()
        {
            using var http = _factory.CreateAuthenticatedClient(
                $"shelter-contract-{Guid.NewGuid():N}", ["shelters.write"], AnimalApiFactory.StaffRoles);
            var shelters = new ClientServices.SheltersApiClient(http);
            var request = NewRequest("Contract Shelter");

            var created = await shelters.CreateShelterAsync(request);
            AssertShelter(created, request);
            var listed = await shelters.GetSheltersAsync();
            AssertShelter(Assert.Single(listed, item => item.Id == created.Id), request);

            var updatedRequest = NewRequest("Contract Shelter Updated");
            await shelters.UpdateShelterAsync(created.Id, updatedRequest);
            var updated = await shelters.GetShelterAsync(created.Id);
            Assert.NotNull(updated);
            AssertShelter(updated, updatedRequest);

            await shelters.DeleteShelterAsync(created.Id);
            Assert.Null(await shelters.GetShelterAsync(created.Id));
        }

        private static ClientModels.ShelterRequest NewRequest(string name) => new()
        {
            Name = name, Address = "12 Contract Way", PhoneNumber = "555-0188",
            Email = "contract-shelter@example.test"
        };

        private static void AssertShelter(
            ClientModels.ShelterResponse actual, ClientModels.ShelterRequest expected)
        {
            Assert.True(actual.Id > 0);
            Assert.Equal(expected.Name, actual.Name);
            Assert.Equal(expected.Address, actual.Address);
            Assert.Equal(expected.PhoneNumber, actual.PhoneNumber);
            Assert.Equal(expected.Email, actual.Email);
        }
    }
}