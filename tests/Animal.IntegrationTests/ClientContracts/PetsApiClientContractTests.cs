extern alias ClientApp;

using ClientModels = ClientApp::Animal.Client.Contracts;
using ClientServices = ClientApp::Animal.Client.Services;
using Animal.IntegrationTests.Api;

namespace Animal.IntegrationTests.ClientContracts
{
    public sealed class PetsApiClientContractTests : IClassFixture<AnimalApiFactory>
    {
        private readonly AnimalApiFactory _factory;
        public PetsApiClientContractTests(AnimalApiFactory factory) => _factory = factory;

        [Fact]
        public async Task PetRequests_AreAcceptedAndEveryResponseFieldRoundTrips()
        {
            using var publicHttp = _factory.CreateClient();
            var shelters = await new ClientServices.SheltersApiClient(publicHttp).GetSheltersAsync();
            var shelter = shelters.First();

            using var staffHttp = _factory.CreateAuthenticatedClient(
                $"pet-contract-{Guid.NewGuid():N}", ["pets.write"], AnimalApiFactory.StaffRoles);
            var pets = new ClientServices.PetsApiClient(staffHttp);
            var request = new ClientModels.PetRequest
            {
                Name = "Contract Fox", Species = "Fennec Fox", Breed = "Sandy",
                Age = 4, IsAvailable = true, ShelterId = shelter.Id
            };

            var created = await pets.CreatePetAsync(request);
            AssertPet(created, request);

            var searched = await pets.GetPetsAsync("Fennec Fox", 4, availableOnly: true);
            AssertPet(Assert.Single(searched), request);

            var updatedRequest = new ClientModels.PetRequest
            {
                Name = "Contract Fox Updated", Species = "Fennec Fox", Breed = "Pale Sandy",
                Age = 5, IsAvailable = false, ShelterId = shelter.Id
            };
            await pets.UpdatePetAsync(created.Id, updatedRequest);

            var updated = await pets.GetPetAsync(created.Id);
            Assert.NotNull(updated);
            AssertPet(updated, updatedRequest);

            await pets.DeletePetAsync(created.Id);
            Assert.Null(await pets.GetPetAsync(created.Id));
        }

        private static void AssertPet(ClientModels.PetResponse actual, ClientModels.PetRequest expected)
        {
            Assert.True(actual.Id > 0);
            Assert.Equal(expected.Name, actual.Name);
            Assert.Equal(expected.Species, actual.Species);
            Assert.Equal(expected.Breed, actual.Breed);
            Assert.Equal(expected.Age, actual.Age);
            Assert.Equal(expected.IsAvailable, actual.IsAvailable);
            Assert.Equal(expected.ShelterId, actual.ShelterId);
        }
    }
}