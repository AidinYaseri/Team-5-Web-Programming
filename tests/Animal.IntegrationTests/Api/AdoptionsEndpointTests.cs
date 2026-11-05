using System.Net;
using System.Net.Http.Json;
using Animal.API.Contracts;
using Animal.Data.Context;
using Animal.Domain.Entities;
using Animal.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Animal.IntegrationTests.Api
{
    // Users 51 and 52 are adopters with profiles. The reviewer ("staff-1") has adoptions.review and no profile.
    // Pets: 1 Biscuit and 2 Mochi are available, 3 Rex is already adopted.
    public class AdoptionsEndpointTests : IClassFixture<AnimalApiFactory>
    {
        private const string Reviewer = "staff-1";
        private readonly AnimalApiFactory _factory;

        public AdoptionsEndpointTests(AnimalApiFactory factory)
        {
            _factory = factory;
            // Users first: it also clears adoption applications, which point at pets.
            factory.ResetUserData([User(51), User(52)]);
            factory.ResetPets(
            [
                new Pet { Id = 1, Name = "Biscuit", Species = "Dog", Breed = "Beagle", Age = 3, IsAvailable = true, ShelterId = 1 },
                new Pet { Id = 2, Name = "Mochi", Species = "Cat", Breed = "Siamese", Age = 1, IsAvailable = true, ShelterId = 1 },
                new Pet { Id = 3, Name = "Rex", Species = "Dog", Breed = "German Shepherd", Age = 7, IsAvailable = false, ShelterId = 2 }
            ]);
        }

        private HttpClient Adopter(int userId, params string[] scopes) =>
            _factory.CreateAuthenticatedClient(userId.ToString(System.Globalization.CultureInfo.InvariantCulture),
                scopes.Length == 0 ? ["adoptions.read", "adoptions.write"] : scopes);

        private HttpClient ReviewerClient() => _factory.CreateAuthenticatedClient(Reviewer, ["adoptions.review"]);

        private static async Task<AdoptionResponse> ApplyAsync(HttpClient client, int petId)
        {
            var response = await client.PostAsJsonAsync("/api/adoptions", new CreateAdoptionRequest { PetId = petId });
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            return (await response.Content.ReadFromJsonAsync<AdoptionResponse>())!;
        }

        [Fact]
        public async Task Apply_WithoutToken_ReturnsUnauthorized()
        {
            using var client = _factory.CreateClient();

            var response = await client.PostAsJsonAsync("/api/adoptions", new CreateAdoptionRequest { PetId = 1 });

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task Apply_WithoutWriteScope_ReturnsForbidden()
        {
            using var client = Adopter(51, "adoptions.read");

            var response = await client.PostAsJsonAsync("/api/adoptions", new CreateAdoptionRequest { PetId = 1 });

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task Apply_SignedInWithoutProfile_ReturnsForbidden()
        {
            using var client = _factory.CreateAuthenticatedClient("no-profile-yet", ["adoptions.write"]);

            var response = await client.PostAsJsonAsync("/api/adoptions", new CreateAdoptionRequest { PetId = 1 });

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task Apply_AvailablePet_ReturnsCreatedPendingWithStringStatusAndLocation()
        {
            using var client = Adopter(51);

            var response = await client.PostAsJsonAsync("/api/adoptions", new CreateAdoptionRequest { PetId = 1 });

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            var json = await response.Content.ReadAsStringAsync();
            Assert.Contains("\"status\":\"Pending\"", json);
            var created = await response.Content.ReadFromJsonAsync<AdoptionResponse>();
            Assert.NotNull(created);
            Assert.Equal("Biscuit", created.PetName);
            Assert.Equal(51, created.UserId);
            Assert.EndsWith($"/api/adoptions/{created.Id}", response.Headers.Location?.ToString(), StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task Apply_UnavailablePet_ReturnsConflict()
        {
            using var client = Adopter(51);

            var response = await client.PostAsJsonAsync("/api/adoptions", new CreateAdoptionRequest { PetId = 3 });

            Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        }

        [Fact]
        public async Task Apply_SecondOpenApplicationForSamePet_ReturnsConflict()
        {
            using var client = Adopter(51);
            await ApplyAsync(client, 1);

            var second = await client.PostAsJsonAsync("/api/adoptions", new CreateAdoptionRequest { PetId = 1 });

            Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
        }

        [Fact]
        public async Task Apply_TwoDifferentAdoptersForSamePet_BothCreated()
        {
            using var first = Adopter(51);
            using var second = Adopter(52);

            await ApplyAsync(first, 1);
            await ApplyAsync(second, 1);
        }

        [Fact]
        public async Task Apply_UnknownPet_ReturnsNotFound()
        {
            using var client = Adopter(51);

            var response = await client.PostAsJsonAsync("/api/adoptions", new CreateAdoptionRequest { PetId = 999 });

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task GetMine_ReturnsOnlyOwnApplications()
        {
            using var user51 = Adopter(51);
            using var user52 = Adopter(52);
            var mine = await ApplyAsync(user51, 1);
            await ApplyAsync(user52, 2);

            var result = await user51.GetFromJsonAsync<List<AdoptionResponse>>("/api/adoptions/mine");

            Assert.NotNull(result);
            Assert.Equal([mine.Id], result.Select(a => a.Id));
        }

        [Fact]
        public async Task GetById_OwnApplication_ReturnsIt_OtherUsersReadsAsNotFound_ReviewerSeesIt()
        {
            using var owner = Adopter(51);
            using var otherUser = Adopter(52);
            using var reviewer = ReviewerClient();
            var created = await ApplyAsync(owner, 1);

            Assert.Equal(HttpStatusCode.OK, (await owner.GetAsync($"/api/adoptions/{created.Id}")).StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, (await otherUser.GetAsync($"/api/adoptions/{created.Id}")).StatusCode);
            Assert.Equal(HttpStatusCode.OK, (await reviewer.GetAsync($"/api/adoptions/{created.Id}")).StatusCode);
        }

        [Fact]
        public async Task Queue_RequiresReviewScope()
        {
            using var adopter = Adopter(51);

            var response = await adopter.GetAsync("/api/adoptions?status=Pending");

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task Queue_ReviewerSeesPendingApplications()
        {
            using var user51 = Adopter(51);
            using var reviewer = ReviewerClient();
            var created = await ApplyAsync(user51, 2);

            var queue = await reviewer.GetFromJsonAsync<List<AdoptionResponse>>("/api/adoptions?status=Pending");

            Assert.NotNull(queue);
            Assert.Contains(queue, a => a.Id == created.Id && a.Status == "Pending" && a.PetName == "Mochi");
        }

        [Fact]
        public async Task Queue_InvalidStatus_ReturnsBadRequest()
        {
            using var reviewer = ReviewerClient();

            var response = await reviewer.GetAsync("/api/adoptions?status=Maybe");

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        // The core flow: approving one application adopts the pet, rejects the other pending one,
        // and notifies both applicants.
        [Fact]
        public async Task Approve_AdoptsPetRejectsOtherPendingAndNotifiesEveryone()
        {
            using var user51 = Adopter(51);
            using var user52 = Adopter(52);
            using var reviewer = ReviewerClient();
            var winner = await ApplyAsync(user51, 1);
            var other = await ApplyAsync(user52, 1);

            var approve = await reviewer.PostAsync($"/api/adoptions/{winner.Id}/approve", content: null);

            Assert.Equal(HttpStatusCode.NoContent, approve.StatusCode);
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AnimalDbContext>();
            Assert.False((await context.Pets.SingleAsync(p => p.Id == 1)).IsAvailable);
            Assert.Equal(AdoptionStatus.Approved, (await context.AdoptionApplications.SingleAsync(a => a.Id == winner.Id)).Status);
            Assert.Equal(AdoptionStatus.Rejected, (await context.AdoptionApplications.SingleAsync(a => a.Id == other.Id)).Status);
            Assert.Contains(context.Notifications, n => n.UserId == 51 && n.Message.Contains("approved"));
            Assert.Contains(context.Notifications, n => n.UserId == 52 && n.Message.Contains("not accepted"));
        }

        [Fact]
        public async Task Approve_OrReject_WhenNotPending_ReturnsConflict()
        {
            using var user51 = Adopter(51);
            using var reviewer = ReviewerClient();
            var created = await ApplyAsync(user51, 2);
            Assert.Equal(HttpStatusCode.NoContent, (await reviewer.PostAsync($"/api/adoptions/{created.Id}/reject", content: null)).StatusCode);

            Assert.Equal(HttpStatusCode.Conflict, (await reviewer.PostAsync($"/api/adoptions/{created.Id}/approve", content: null)).StatusCode);
            Assert.Equal(HttpStatusCode.Conflict, (await reviewer.PostAsync($"/api/adoptions/{created.Id}/reject", content: null)).StatusCode);
        }

        [Fact]
        public async Task Reject_NotifiesApplicantAndKeepsPetAvailable()
        {
            using var user51 = Adopter(51);
            using var reviewer = ReviewerClient();
            var created = await ApplyAsync(user51, 2);

            var reject = await reviewer.PostAsync($"/api/adoptions/{created.Id}/reject", content: null);

            Assert.Equal(HttpStatusCode.NoContent, reject.StatusCode);
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AnimalDbContext>();
            Assert.True((await context.Pets.SingleAsync(p => p.Id == 2)).IsAvailable);
            Assert.Contains(context.Notifications, n => n.UserId == 51 && n.Message.Contains("not accepted"));
        }

        [Fact]
        public async Task ApproveAndReject_AsAdopterWithoutReviewScope_ReturnForbidden()
        {
            using var user51 = Adopter(51);
            var created = await ApplyAsync(user51, 1);

            Assert.Equal(HttpStatusCode.Forbidden, (await user51.PostAsync($"/api/adoptions/{created.Id}/approve", content: null)).StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, (await user51.PostAsync($"/api/adoptions/{created.Id}/reject", content: null)).StatusCode);
        }

        [Fact]
        public async Task Approve_UnknownApplication_ReturnsNotFound()
        {
            using var reviewer = ReviewerClient();

            var response = await reviewer.PostAsync("/api/adoptions/999/approve", content: null);

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        private static User User(int id) => new()
        {
            Id = id,
            Name = $"Adopter {id}",
            Email = $"adopter{id}@example.test",
            PhoneNumber = "555-0100"
        };
    }
}
