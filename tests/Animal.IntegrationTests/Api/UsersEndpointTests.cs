using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Animal.API.Contracts;
using Animal.Data.Context;
using Animal.Data.Repositories;
using Animal.Domain.Entities;
using Animal.Domain.Services;
using Animal.IntegrationTests.Api.TestDoubles;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace Animal.IntegrationTests.Api
{
    public class UsersEndpointTests : IClassFixture<AnimalApiFactory>
    {
        private readonly AnimalApiFactory _factory;

        public UsersEndpointTests(AnimalApiFactory factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task GetMyProfile_BeforePut_ReturnsNotFound()
        {
            var identity = NewIdentity();
            using var client = _factory.CreateAuthenticatedClient(identity.Subject, ["users.read"]);

            var response = await client.GetAsync("/api/users/me");

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
            Assert.Contains("You don't have a profile yet.", await response.Content.ReadAsStringAsync());
        }

        [Fact]
        public async Task PutThenGetMyProfile_ReturnsSavedProfileWithoutExternalId()
        {
            var identity = NewIdentity();
            using var client = _factory.CreateAuthenticatedClient(identity.Subject, ["users.read", "users.write"]);
            var request = NewRequest(identity.Email);

            var put = await client.PutAsJsonAsync("/api/users/me", request);
            var get = await client.GetAsync("/api/users/me");
            var profile = await get.Content.ReadFromJsonAsync<UserResponse>();

            Assert.Equal(HttpStatusCode.NoContent, put.StatusCode);
            Assert.Equal(HttpStatusCode.OK, get.StatusCode);
            Assert.Equal(request.Name, profile!.Name);
            Assert.Equal(identity.Email, profile.Email);
            Assert.False(profile.HasPicture);
        }

        [Fact]
        public async Task PutMyProfile_Again_UpdatesExistingRowOnly()
        {
            var identity = NewIdentity();
            using var client = _factory.CreateAuthenticatedClient(identity.Subject, ["users.write"]);

            Assert.Equal(HttpStatusCode.NoContent,
                (await client.PutAsJsonAsync("/api/users/me", NewRequest(identity.Email))).StatusCode);
            var changed = NewRequest(identity.Email) with { Name = "Changed Name" };

            Assert.Equal(HttpStatusCode.NoContent,
                (await client.PutAsJsonAsync("/api/users/me", changed)).StatusCode);

            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AnimalDbContext>();
            Assert.Equal(1, context.Users.Count(user => user.ExternalId == identity.Subject));
            Assert.Equal("Changed Name", context.Users.Single(user => user.ExternalId == identity.Subject).Name);
        }

        [Fact]
        public async Task PutMyProfile_WithOwnUnchangedEmail_ReturnsNoContent()
        {
            var identity = NewIdentity();
            using var client = _factory.CreateAuthenticatedClient(identity.Subject, ["users.write"]);

            await client.PutAsJsonAsync("/api/users/me", NewRequest(identity.Email));
            var response = await client.PutAsJsonAsync("/api/users/me", NewRequest(identity.Email.ToUpperInvariant()));

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        }

        [Fact]
        public async Task PutMyProfile_WithAnotherUsersEmail_ReturnsConflict()
        {
            var first = NewIdentity();
            var second = NewIdentity();
            using var firstClient = _factory.CreateAuthenticatedClient(first.Subject, ["users.write"]);
            using var secondClient = _factory.CreateAuthenticatedClient(second.Subject, ["users.write"]);

            await firstClient.PutAsJsonAsync("/api/users/me", NewRequest(first.Email));
            var response = await secondClient.PutAsJsonAsync("/api/users/me", NewRequest(first.Email.ToUpperInvariant()));

            Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        }

        [Fact]
        public async Task TwoUsers_GetMyProfile_ReturnsOnlyTheirOwnProfile()
        {
            var first = NewIdentity();
            var second = NewIdentity();
            using var firstClient = _factory.CreateAuthenticatedClient(first.Subject, ["users.read", "users.write"]);
            using var secondClient = _factory.CreateAuthenticatedClient(second.Subject, ["users.read", "users.write"]);

            await firstClient.PutAsJsonAsync("/api/users/me", NewRequest(first.Email));
            await secondClient.PutAsJsonAsync("/api/users/me", NewRequest(second.Email));

            var firstProfile = await firstClient.GetFromJsonAsync<UserResponse>("/api/users/me");
            var secondProfile = await secondClient.GetFromJsonAsync<UserResponse>("/api/users/me");

            Assert.Equal(first.Email, firstProfile!.Email);
            Assert.Equal(second.Email, secondProfile!.Email);
            Assert.NotEqual(firstProfile.Id, secondProfile.Id);
        }

        [Fact]
        public async Task PutMyProfile_WithInvalidBody_ReturnsBadRequest()
        {
            var identity = NewIdentity();
            using var client = _factory.CreateAuthenticatedClient(identity.Subject, ["users.write"]);

            var response = await client.PutAsJsonAsync("/api/users/me",
                new { name = "", email = "not-an-email", phoneNumber = "" });

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task MyProfile_WithoutToken_ReturnsUnauthorized()
        {
            using var client = _factory.CreateClient();

            Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/users/me")).StatusCode);
            Assert.Equal(HttpStatusCode.Unauthorized,
                (await client.PutAsJsonAsync("/api/users/me", NewRequest(NewIdentity().Email))).StatusCode);
            Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/users/me/picture")).StatusCode);
            Assert.Equal(HttpStatusCode.Unauthorized,
                (await client.PutAsync("/api/users/me/picture", CreateImageUpload(TinyPng(), "image/png"))).StatusCode);
            Assert.Equal(HttpStatusCode.Unauthorized, (await client.DeleteAsync("/api/users/me/picture")).StatusCode);
        }

        [Fact]
        public async Task MyProfile_WithOnlyOneUsersScope_ReturnsForbiddenForOtherOperation()
        {
            var identity = NewIdentity();
            using var writeOnly = _factory.CreateAuthenticatedClient(identity.Subject, ["users.write"]);
            using var readOnly = _factory.CreateAuthenticatedClient(NewIdentity().Subject, ["users.read"]);

            Assert.Equal(HttpStatusCode.Forbidden, (await writeOnly.GetAsync("/api/users/me")).StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden,
                (await readOnly.PutAsJsonAsync("/api/users/me", NewRequest(identity.Email))).StatusCode);

            Assert.Equal(HttpStatusCode.Forbidden, (await writeOnly.GetAsync("/api/users/me/picture")).StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden,
                (await readOnly.PutAsync("/api/users/me/picture", CreateImageUpload(TinyPng(), "image/png"))).StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, (await readOnly.DeleteAsync("/api/users/me/picture")).StatusCode);
        }

        [Fact]
        public async Task UserCollectionRoutes_AreNotAvailable()
        {
            var identity = NewIdentity();
            using var client = _factory.CreateAuthenticatedClient(identity.Subject, ["users.read"]);

            Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/api/users")).StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/api/users/1")).StatusCode);
        }

        [Fact]
        public async Task GetMyProfile_DoesNotSerializeExternalId()
        {
            var identity = NewIdentity();
            using var client = _factory.CreateAuthenticatedClient(identity.Subject, ["users.read", "users.write"]);

            await client.PutAsJsonAsync("/api/users/me", NewRequest(identity.Email));
            var response = await client.GetAsync("/api/users/me");

            Assert.DoesNotContain("externalId", await response.Content.ReadAsStringAsync(), StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task PutMyPicture_ValidImage_ReturnsNoContent_AndGetReturnsImageWithCacheControl()
        {
            var identity = NewIdentity();
            using var client = _factory.CreateAuthenticatedClient(identity.Subject, ["users.read", "users.write"]);
            await client.PutAsJsonAsync("/api/users/me", NewRequest(identity.Email));

            var put = await client.PutAsync("/api/users/me/picture", CreateImageUpload(TinyPng(), "image/png"));
            var get = await client.GetAsync("/api/users/me/picture");
            var bytes = await get.Content.ReadAsByteArrayAsync();

            Assert.Equal(HttpStatusCode.NoContent, put.StatusCode);
            Assert.Equal(HttpStatusCode.OK, get.StatusCode);
            Assert.NotNull(get.Headers.CacheControl);
            Assert.True(get.Headers.CacheControl!.Private);
            Assert.True(get.Headers.CacheControl.NoStore);
            Assert.Equal("nosniff", Assert.Single(get.Headers.GetValues("X-Content-Type-Options")));
            Assert.Equal("image/png", get.Content.Headers.ContentType?.MediaType);
            Assert.NotEmpty(bytes);
        }

        [Fact]
        public async Task PutMyPicture_NoProfile_ReturnsNotFound()
        {
            var identity = NewIdentity();
            using var client = _factory.CreateAuthenticatedClient(identity.Subject, ["users.write"]);

            var response = await client.PutAsync("/api/users/me/picture", CreateImageUpload(TinyPng(), "image/png"));

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task GetMyPicture_WhenMissing_ReturnsNotFound()
        {
            var identity = NewIdentity();
            using var client = _factory.CreateAuthenticatedClient(identity.Subject, ["users.read", "users.write"]);
            await client.PutAsJsonAsync("/api/users/me", NewRequest(identity.Email));

            var response = await client.GetAsync("/api/users/me/picture");

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task DeleteMyPicture_RemovesPicture()
        {
            var identity = NewIdentity();
            using var client = _factory.CreateAuthenticatedClient(identity.Subject, ["users.read", "users.write"]);
            await client.PutAsJsonAsync("/api/users/me", NewRequest(identity.Email));
            await client.PutAsync("/api/users/me/picture", CreateImageUpload(TinyPng(), "image/png"));

            var delete = await client.DeleteAsync("/api/users/me/picture");
            var get = await client.GetAsync("/api/users/me/picture");

            Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, get.StatusCode);
        }

        [Fact]
        public async Task PutMyPicture_CorruptImage_ReturnsBadRequest_AndLeavesExistingPictureUnchanged()
        {
            var identity = NewIdentity();
            using var client = _factory.CreateAuthenticatedClient(identity.Subject, ["users.read", "users.write"]);
            await client.PutAsJsonAsync("/api/users/me", NewRequest(identity.Email));
            await client.PutAsync("/api/users/me/picture", CreateImageUpload(TinyPng(), "image/png"));

            var before = await client.GetAsync("/api/users/me/picture");
            var beforeBytes = await before.Content.ReadAsByteArrayAsync();

            var rejected = await client.PutAsync("/api/users/me/picture", CreateImageUpload([1, 2, 3, 4, 5], "image/png", "corrupt.png"));
            var after = await client.GetAsync("/api/users/me/picture");
            var afterBytes = await after.Content.ReadAsByteArrayAsync();

            Assert.Equal(HttpStatusCode.BadRequest, rejected.StatusCode);
            Assert.Equal(HttpStatusCode.OK, after.StatusCode);
            Assert.Equal(before.Content.Headers.ContentType?.MediaType, after.Content.Headers.ContentType?.MediaType);
            Assert.Equal(beforeBytes, afterBytes);
        }

        [Fact]
        public async Task PutMyPicture_Gif_ReturnsBadRequest_AndLeavesExistingPictureUnchanged()
        {
            var identity = NewIdentity();
            using var client = _factory.CreateAuthenticatedClient(identity.Subject, ["users.read", "users.write"]);
            await client.PutAsJsonAsync("/api/users/me", NewRequest(identity.Email));
            await client.PutAsync("/api/users/me/picture", CreateImageUpload(TinyPng(), "image/png"));

            var before = await client.GetAsync("/api/users/me/picture");
            var beforeBytes = await before.Content.ReadAsByteArrayAsync();

            var rejected = await client.PutAsync("/api/users/me/picture", CreateImageUpload(TinyGif(), "image/gif", "picture.gif"));
            var after = await client.GetAsync("/api/users/me/picture");
            var afterBytes = await after.Content.ReadAsByteArrayAsync();

            Assert.Equal(HttpStatusCode.BadRequest, rejected.StatusCode);
            Assert.Equal(HttpStatusCode.OK, after.StatusCode);
            Assert.Equal(before.Content.Headers.ContentType?.MediaType, after.Content.Headers.ContentType?.MediaType);
            Assert.Equal(beforeBytes, afterBytes);
        }

        [Fact]
        public async Task PutMyPicture_JpegDeclaredAsPng_ReturnsBadRequest_AndLeavesExistingPictureUnchanged()
        {
            var identity = NewIdentity();
            using var client = _factory.CreateAuthenticatedClient(identity.Subject, ["users.read", "users.write"]);
            await client.PutAsJsonAsync("/api/users/me", NewRequest(identity.Email));
            await client.PutAsync("/api/users/me/picture", CreateImageUpload(TinyPng(), "image/png"));

            var before = await client.GetAsync("/api/users/me/picture");
            var beforeBytes = await before.Content.ReadAsByteArrayAsync();

            var rejected = await client.PutAsync("/api/users/me/picture", CreateImageUpload(TinyJpeg(), "image/png", "picture.jpg"));
            var after = await client.GetAsync("/api/users/me/picture");
            var afterBytes = await after.Content.ReadAsByteArrayAsync();

            Assert.Equal(HttpStatusCode.BadRequest, rejected.StatusCode);
            Assert.Equal(HttpStatusCode.OK, after.StatusCode);
            Assert.Equal(before.Content.Headers.ContentType?.MediaType, after.Content.Headers.ContentType?.MediaType);
            Assert.Equal(beforeBytes, afterBytes);
        }

        [Fact]
        public async Task PutMyPicture_DimensionsOver2048_ReturnsBadRequest_AndLeavesExistingPictureUnchanged()
        {
            var identity = NewIdentity();
            using var client = _factory.CreateAuthenticatedClient(identity.Subject, ["users.read", "users.write"]);
            await client.PutAsJsonAsync("/api/users/me", NewRequest(identity.Email));
            await client.PutAsync("/api/users/me/picture", CreateImageUpload(TinyPng(), "image/png"));

            var before = await client.GetAsync("/api/users/me/picture");
            var beforeBytes = await before.Content.ReadAsByteArrayAsync();

            var rejected = await client.PutAsync("/api/users/me/picture", CreateImageUpload(WidePng3000(), "image/png", "wide.png"));
            var after = await client.GetAsync("/api/users/me/picture");
            var afterBytes = await after.Content.ReadAsByteArrayAsync();

            Assert.Equal(HttpStatusCode.BadRequest, rejected.StatusCode);
            Assert.Equal(HttpStatusCode.OK, after.StatusCode);
            Assert.Equal(before.Content.Headers.ContentType?.MediaType, after.Content.Headers.ContentType?.MediaType);
            Assert.Equal(beforeBytes, afterBytes);
        }

        [Fact]
        public async Task PutMyPicture_Over2Mb_ReturnsBadRequest_AndLeavesExistingPictureUnchanged()
        {
            var identity = NewIdentity();
            using var client = _factory.CreateAuthenticatedClient(identity.Subject, ["users.read", "users.write"]);
            await client.PutAsJsonAsync("/api/users/me", NewRequest(identity.Email));
            await client.PutAsync("/api/users/me/picture", CreateImageUpload(TinyPng(), "image/png"));

            var before = await client.GetAsync("/api/users/me/picture");
            var beforeBytes = await before.Content.ReadAsByteArrayAsync();

            var payload = new byte[ProfilePictureRules.MaxBytes + 1];
            payload[0] = 0x89;
            payload[1] = 0x50;
            payload[2] = 0x4E;
            payload[3] = 0x47;
            payload[4] = 0x0D;
            payload[5] = 0x0A;
            payload[6] = 0x1A;
            payload[7] = 0x0A;

            var rejected = await client.PutAsync("/api/users/me/picture", CreateImageUpload(payload, "image/png", "over-2mb.png"));
            var after = await client.GetAsync("/api/users/me/picture");
            var afterBytes = await after.Content.ReadAsByteArrayAsync();

            Assert.Equal(HttpStatusCode.BadRequest, rejected.StatusCode);
            Assert.Equal(HttpStatusCode.OK, after.StatusCode);
            Assert.Equal(before.Content.Headers.ContentType?.MediaType, after.Content.Headers.ContentType?.MediaType);
            Assert.Equal(beforeBytes, afterBytes);
        }

        [Fact]
        public async Task PutMyPicture_RequestOver3Mb_ReturnsPayloadTooLarge_AndLeavesExistingPictureUnchanged()
        {
            var identity = NewIdentity();
            using var client = _factory.CreateAuthenticatedClient(identity.Subject, ["users.read", "users.write"]);
            await client.PutAsJsonAsync("/api/users/me", NewRequest(identity.Email));
            await client.PutAsync("/api/users/me/picture", CreateImageUpload(TinyPng(), "image/png"));

            var before = await client.GetAsync("/api/users/me/picture");
            var beforeBytes = await before.Content.ReadAsByteArrayAsync();

            var payload = new byte[(3 * 1024 * 1024) + 100_000];
            payload[0] = 1;
            var rejected = await client.PutAsync("/api/users/me/picture", CreateImageUpload(payload, "application/octet-stream", "large.bin"));
            var after = await client.GetAsync("/api/users/me/picture");
            var afterBytes = await after.Content.ReadAsByteArrayAsync();

            Assert.Equal(HttpStatusCode.RequestEntityTooLarge, rejected.StatusCode);
            Assert.Equal(HttpStatusCode.OK, after.StatusCode);
            Assert.Equal(before.Content.Headers.ContentType?.MediaType, after.Content.Headers.ContentType?.MediaType);
            Assert.Equal(beforeBytes, afterBytes);
        }

        [Fact]
        public async Task GetMyProfile_WhenPictureExists_ReturnsHasPictureTrue()
        {
            var identity = NewIdentity();
            using var client = _factory.CreateAuthenticatedClient(identity.Subject, ["users.read", "users.write"]);
            await client.PutAsJsonAsync("/api/users/me", NewRequest(identity.Email));

            var before = await client.GetFromJsonAsync<UserResponse>("/api/users/me");
            await client.PutAsync("/api/users/me/picture", CreateImageUpload(TinyPng(), "image/png"));
            var after = await client.GetFromJsonAsync<UserResponse>("/api/users/me");

            Assert.False(before!.HasPicture);
            Assert.True(after!.HasPicture);
        }

        [Fact]
        public async Task PutMyPicture_WhenStoreThrows_ReturnsSanitizedProblemDetails_AndKeepsProfileAndPictureUnchanged()
        {
            using var factory = new AnimalApiFactory(services =>
            {
                services.RemoveAll<IProfilePictureStore>();
                services.AddScoped<IProfilePictureStore, ThrowingProfilePictureStore>();
            });

            var identity = NewIdentity();
            using (var arrangeScope = factory.Services.CreateScope())
            {
                var arrangeContext = arrangeScope.ServiceProvider.GetRequiredService<AnimalDbContext>();
                var seededUser = new User
                {
                    ExternalId = identity.Subject,
                    Name = "Seeded User",
                    Email = identity.Email,
                    PhoneNumber = "555-0100"
                };
                arrangeContext.Users.Add(seededUser);
                await arrangeContext.SaveChangesAsync();

                arrangeContext.ProfilePictures.Add(new ProfilePicture
                {
                    UserId = seededUser.Id,
                    ContentType = "image/png",
                    Bytes = TinyPng(),
                    UpdatedAt = DateTime.UtcNow
                });
                await arrangeContext.SaveChangesAsync();
            }

            using var beforeScope = factory.Services.CreateScope();
            var beforeContext = beforeScope.ServiceProvider.GetRequiredService<AnimalDbContext>();
            var beforeUser = await beforeContext.Users.AsNoTracking().SingleAsync(user => user.ExternalId == identity.Subject);
            var beforePicture = await beforeContext.ProfilePictures.AsNoTracking().SingleAsync(row => row.UserId == beforeUser.Id);

            using var failingClient = factory.CreateAuthenticatedClient(identity.Subject, ["users.read", "users.write"]);
            var failed = await failingClient.PutAsync("/api/users/me/picture", CreateImageUpload(TinyJpeg(), "image/jpeg", "new.jpg"));
            var body = await failed.Content.ReadAsStringAsync();

            Assert.True(failed.StatusCode is HttpStatusCode.InternalServerError or HttpStatusCode.ServiceUnavailable);
            Assert.Contains("type", body, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("title", body, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("Server=", body, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("Password=", body, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("SELECT", body, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("\\", body, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("stack", body, StringComparison.OrdinalIgnoreCase);

            using var afterScope = factory.Services.CreateScope();
            var afterContext = afterScope.ServiceProvider.GetRequiredService<AnimalDbContext>();
            var afterUser = await afterContext.Users.AsNoTracking().SingleAsync(user => user.ExternalId == identity.Subject);
            var afterPicture = await afterContext.ProfilePictures.AsNoTracking().SingleAsync(row => row.UserId == afterUser.Id);

            Assert.Equal(beforeUser.Name, afterUser.Name);
            Assert.Equal(beforeUser.Email, afterUser.Email);
            Assert.Equal(beforeUser.PhoneNumber, afterUser.PhoneNumber);
            Assert.Equal(beforePicture.ContentType, afterPicture.ContentType);
            Assert.Equal(beforePicture.Bytes, afterPicture.Bytes);
        }

        [Fact]
        public async Task PutMyPicture_WhenUnitOfWorkThrows_RollsBackPictureAndProfileMutation()
        {
            using var factory = new AnimalApiFactory(services =>
            {
                services.RemoveAll<IProfilePictureStore>();
                services.RemoveAll<IUnitOfWork>();
                services.AddScoped<IProfilePictureStore, MutatingProfilePictureStore>();
                services.AddScoped<IUnitOfWork, ThrowingUnitOfWork>();
            });

            var identity = NewIdentity();
            using (var arrangeScope = factory.Services.CreateScope())
            {
                var arrangeContext = arrangeScope.ServiceProvider.GetRequiredService<AnimalDbContext>();
                var seededUser = new User
                {
                    ExternalId = identity.Subject,
                    Name = "Seeded User",
                    Email = identity.Email,
                    PhoneNumber = "555-0100"
                };
                arrangeContext.Users.Add(seededUser);
                await arrangeContext.SaveChangesAsync();

                arrangeContext.ProfilePictures.Add(new ProfilePicture
                {
                    UserId = seededUser.Id,
                    ContentType = "image/png",
                    Bytes = TinyPng(),
                    UpdatedAt = DateTime.UtcNow
                });
                await arrangeContext.SaveChangesAsync();
            }

            using var client = factory.CreateAuthenticatedClient(identity.Subject, ["users.read", "users.write"]);
            var beforeProfile = await client.GetFromJsonAsync<UserResponse>("/api/users/me");
            using var beforeScope = factory.Services.CreateScope();
            var beforeContext = beforeScope.ServiceProvider.GetRequiredService<AnimalDbContext>();
            var beforePicture = await beforeContext.ProfilePictures.AsNoTracking().SingleAsync(row => row.UserId == beforeProfile!.Id);

            var failed = await client.PutAsync("/api/users/me/picture", CreateImageUpload(TinyJpeg(), "image/jpeg", "rollback.jpg"));
            var body = await failed.Content.ReadAsStringAsync();

            Assert.True(failed.StatusCode is HttpStatusCode.InternalServerError or HttpStatusCode.ServiceUnavailable);
            Assert.DoesNotContain("Password=", body, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("Server=", body, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("timeout", body, StringComparison.OrdinalIgnoreCase);

            var afterProfile = await client.GetFromJsonAsync<UserResponse>("/api/users/me");
            using var afterScope = factory.Services.CreateScope();
            var afterContext = afterScope.ServiceProvider.GetRequiredService<AnimalDbContext>();
            var afterPicture = await afterContext.ProfilePictures.AsNoTracking().SingleAsync(row => row.UserId == afterProfile!.Id);

            Assert.Equal(beforeProfile.Name, afterProfile.Name);
            Assert.Equal(beforeProfile.Email, afterProfile.Email);
            Assert.Equal(beforeProfile.PhoneNumber, afterProfile.PhoneNumber);
            Assert.Equal(beforePicture.ContentType, afterPicture.ContentType);
            Assert.Equal(beforePicture.Bytes, afterPicture.Bytes);
        }

        [Fact]
        public async Task PutMyPicture_LogsDoNotContainTokenEmailOrImageBytes()
        {
            var loggerProvider = new InMemoryLoggerProvider();
            var identity = NewIdentity();
            var requestBytes = TinyPng();
            var token = string.Empty;

            using var factory = new AnimalApiFactory(logging =>
            {
                logging.ClearProviders();
                logging.AddProvider(loggerProvider);
            });

            token = factory.CreateToken(subject: identity.Subject, scopes: ["users.read", "users.write"]);
            using var client = factory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            await client.PutAsJsonAsync("/api/users/me", NewRequest(identity.Email));
            await client.PutAsync("/api/users/me/picture", CreateImageUpload(requestBytes, "image/png"));

            var logs = string.Join("\n", loggerProvider.Entries);
            var byteMarker = Convert.ToBase64String(requestBytes.Take(Math.Min(24, requestBytes.Length)).ToArray());

            Assert.DoesNotContain(identity.Email, logs, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain(token, logs, StringComparison.Ordinal);
            Assert.DoesNotContain(byteMarker, logs, StringComparison.Ordinal);
        }

        private static MultipartFormDataContent CreateImageUpload(byte[] bytes, string contentType, string fileName = "picture.png")
        {
            var content = new MultipartFormDataContent();
            var file = new ByteArrayContent(bytes);
            file.Headers.ContentType = MediaTypeHeaderValue.Parse(contentType);
            content.Add(file, "file", fileName);
            return content;
        }

        private static byte[] TinyPng() => ReadTestImage("profile-picture.png");

        private static byte[] TinyJpeg() => ReadTestImage("profile-picture.jpg");

        private static byte[] TinyGif() => ReadTestImage("profile-picture.gif");

        private static byte[] WidePng3000() => ReadTestImage("profile-picture-wide-3000.png");

        private static byte[] ReadTestImage(string fileName)
        {
            var path = Path.Combine(AppContext.BaseDirectory, "TestData", fileName);
            return File.ReadAllBytes(path);
        }

        private static UserIdentity NewIdentity()
        {
            var value = Guid.NewGuid().ToString("N");
            return new UserIdentity($"subject-{value}", $"{value}@example.com");
        }

        private static UpdateUserRequest NewRequest(string email) =>
            new() { Name = "Test User", Email = email, PhoneNumber = "555-0100" };

        private sealed record UserIdentity(string Subject, string Email);
    }
}
