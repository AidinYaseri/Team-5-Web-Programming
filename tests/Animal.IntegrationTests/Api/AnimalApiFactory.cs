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
            return CreateAuthenticatedClient("test-subject", ["pets.write"]);
        }

        public HttpClient CreateAuthenticatedClient(string subject, IEnumerable<string> scopes)
        {
            var client = CreateClient();
            client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue(
                    "Bearer",
                    _tokenFactory.CreateToken(subject: subject, scopes: scopes));
            return client;
        }

        public string CreateToken(
            string? issuer = null,
            string? audience = null,
            DateTimeOffset? expiresAt = null,
            string? subject = "test-subject",
            IEnumerable<string>? scopes = null,
            string scopeClaimType = "scope",
            RSA? signingKey = null) =>
            _tokenFactory.CreateToken(
                issuer,
                audience,
                expiresAt,
                subject,
                scopes,
                scopeClaimType,
                signingKey);

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
