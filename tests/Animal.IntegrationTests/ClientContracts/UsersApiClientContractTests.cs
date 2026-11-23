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
        public async Task ProfilePictureRequests_AreAcceptedAndClientReadsReturnedImage()
        {
            var suffix = Guid.NewGuid().ToString("N");
            using var http = _factory.CreateAuthenticatedClient(
                $"picture-contract-{suffix}", ["users.read", "users.write"]);
            var users = new ClientServices.UsersApiClient(http);

            await users.UpdateMyProfileAsync(new ClientModels.UpdateUserRequest
            {
                Name = "Picture Contract User",
                Email = $"{suffix}@example.test",
                PhoneNumber = "555-0192"
            });

            var imagePath = Path.Combine(AppContext.BaseDirectory, "TestData", "profile-picture.png");
            await using (var imageStream = File.OpenRead(imagePath))
            {
                await users.UploadMyProfilePictureAsync(imageStream, "profile.png", "image/png");
            }

            var picture = await users.GetMyProfilePictureAsync();

            Assert.NotNull(picture);
            Assert.Equal("image/png", picture.ContentType);
            Assert.True(picture.Bytes.Length > 8);
            Assert.Equal(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }, picture.Bytes[..8]);

            await users.DeleteMyProfilePictureAsync();
            Assert.Null(await users.GetMyProfilePictureAsync());
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
