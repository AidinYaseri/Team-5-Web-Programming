using Animal.Data.Context;
using Animal.Domain.Entities;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

namespace Animal.IntegrationTests.Api
{
    // Runs the real Program.cs, but swaps SQL Server for SQLite in memory so the tests need no database server.
    // Everything else (repositories, PetService, exception handler) is the real registration.
    public class AnimalApiFactory : WebApplicationFactory<Program>
    {
        // Kept open for the factory's lifetime: an in-memory SQLite database disappears when its last connection closes.
        private readonly SqliteConnection _connection = new("DataSource=:memory:");
        private readonly TestTokenFactory _tokenFactory = new();

        public AnimalApiFactory()
        {
            _connection.Open();
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureTestServices(services =>
            {
                // Both are needed: EF Core 9+ keeps the UseSqlServer call in IDbContextOptionsConfiguration.
                services.RemoveAll<DbContextOptions<AnimalDbContext>>();
                services.RemoveAll<IDbContextOptionsConfiguration<AnimalDbContext>>();
                services.AddDbContext<AnimalDbContext>(options => options.UseSqlite(_connection));
                services.PostConfigure<JwtBearerOptions>(
                    JwtBearerDefaults.AuthenticationScheme,
                    options =>
                {
                    var signingKey = _tokenFactory.SigningKey;
                    var configuration = new OpenIdConnectConfiguration
                    {
                        Issuer = TestTokenFactory.Issuer
                    };
                    configuration.SigningKeys.Add(signingKey);

                    options.Authority = TestTokenFactory.Issuer;
                    options.Audience = TestTokenFactory.Audience;
                    options.RequireHttpsMetadata = false;
                    options.ConfigurationManager =
                        new StaticConfigurationManager<OpenIdConnectConfiguration>(configuration);
                    options.TokenValidationParameters.ValidIssuer = TestTokenFactory.Issuer;
                    options.TokenValidationParameters.ValidAudience = TestTokenFactory.Audience;
                    options.TokenValidationParameters.IssuerSigningKey = signingKey;
                    options.TokenValidationParameters.IssuerSigningKeys = [signingKey];
                });
            });
        }

        public HttpClient CreateAuthenticatedClient()
        {
            return CreateAuthenticatedClient("test-subject", ["pets.write"], StaffRoles);
        }

        // One "device" signing in on its own: a fresh token (its own jti and issue time) for the given user.
        // Used by the cross-device tests, which must never copy one device's token to another.
        public DeviceSession SignInDevice(string subject, IEnumerable<string> scopes, TimeSpan issuedAgo = default)
        {
            var tokenId = Guid.NewGuid().ToString("N");
            var token = _tokenFactory.CreateToken(subject: subject, scopes: scopes, tokenId: tokenId,
                issuedAt: DateTimeOffset.UtcNow - issuedAgo);
            var client = CreateClient();
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            return new DeviceSession(client, token, tokenId);
        }

        public sealed record DeviceSession(HttpClient Client, string Token, string TokenId) : IDisposable
        {
            public void Dispose() => Client.Dispose();
        }

        // Staff operations (pets.write, shelters.write, adoptions.review) need this role as well as the scope.
        public static readonly string[] StaffRoles = ["staff"];

        public HttpClient CreateAuthenticatedClient(string subject, IEnumerable<string> scopes, IEnumerable<string>? roles = null)
        {
            var client = CreateClient();
            client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue(
                    "Bearer",
                    _tokenFactory.CreateToken(subject: subject, scopes: scopes, roles: roles));
            return client;
        }

        public string CreateToken(
            string? issuer = null,
            string? audience = null,
            DateTimeOffset? expiresAt = null,
            string? subject = "test-subject",
            IEnumerable<string>? scopes = null,
            string scopeClaimType = "scope",
            RSA? signingKey = null,
            IEnumerable<System.Security.Claims.Claim>? extraClaims = null,
            IEnumerable<string>? roles = null) =>
            _tokenFactory.CreateToken(
                issuer,
                audience,
                expiresAt,
                subject,
                scopes,
                scopeClaimType,
                signingKey,
                extraClaims,
                roles);

        // Builds the schema and the HasData seed rows (2 shelters, 5 pets) once the host exists.
        protected override IHost CreateHost(IHostBuilder builder)
        {
            var host = base.CreateHost(builder);

            using var scope = host.Services.CreateScope();
            scope.ServiceProvider.GetRequiredService<AnimalDbContext>().Database.EnsureCreated();

            return host;
        }

        // Replaces every pet with the given ones, so a test knows exactly what is in the table.
        public void ResetPets(IEnumerable<Pet> pets)
        {
            using var scope = Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AnimalDbContext>();

            context.Pets.RemoveRange(context.Pets);
            context.SaveChanges();

            context.Pets.AddRange(pets);
            context.SaveChanges();
        }

        // Replaces users, notifications and appointments with the given rows (and removes adoption applications,
        // which also point at users). A user without an ExternalId gets
        // its id as the ExternalId, so a test token with sub = "41" maps to user 41.
        public void ResetUserData(IEnumerable<User> users, IEnumerable<Notification>? notifications = null, IEnumerable<Appointment>? appointments = null)
        {
            using var scope = Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AnimalDbContext>();

            context.Notifications.RemoveRange(context.Notifications);
            context.Appointments.RemoveRange(context.Appointments);
            context.AdoptionApplications.RemoveRange(context.AdoptionApplications);
            context.Users.RemoveRange(context.Users);
            context.SaveChanges();

            var userRows = users.ToList();
            foreach (var user in userRows.Where(user => string.IsNullOrWhiteSpace(user.ExternalId)))
            {
                user.ExternalId = user.Id.ToString(System.Globalization.CultureInfo.InvariantCulture);
            }

            context.Users.AddRange(userRows);
            context.SaveChanges();

            if (notifications is not null)
            {
                context.Notifications.AddRange(notifications);
            }

            if (appointments is not null)
            {
                context.Appointments.AddRange(appointments);
            }

            context.SaveChanges();
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (disposing)
            {
                _tokenFactory.Dispose();
                _connection.Dispose();
            }
        }
    }
}
