using Animal.Data.Context;
using Animal.Domain.Entities;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace Animal.IntegrationTests.Api
{
    // Runs the real Program.cs against SQLite and replaces bearer validation with a deterministic test scheme.
    public class AnimalApiFactory : WebApplicationFactory<Program>
    {
        private readonly SqliteConnection _connection = new("DataSource=:memory:");

        public AnimalApiFactory() => _connection.Open();

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<DbContextOptions<AnimalDbContext>>();
                services.RemoveAll<IDbContextOptionsConfiguration<AnimalDbContext>>();
                services.AddDbContext<AnimalDbContext>(options => options.UseSqlite(_connection));

                services.AddAuthentication(options =>
                {
                    options.DefaultScheme = TestAuthenticationHandler.SchemeName;
                    options.DefaultAuthenticateScheme = TestAuthenticationHandler.SchemeName;
                    options.DefaultChallengeScheme = TestAuthenticationHandler.SchemeName;
                    options.DefaultForbidScheme = TestAuthenticationHandler.SchemeName;
                }).AddScheme<AuthenticationSchemeOptions, TestAuthenticationHandler>(TestAuthenticationHandler.SchemeName, _ => { });
            });
        }

        protected override IHost CreateHost(IHostBuilder builder)
        {
            var host = base.CreateHost(builder);
            using var scope = host.Services.CreateScope();
            scope.ServiceProvider.GetRequiredService<AnimalDbContext>().Database.EnsureCreated();
            return host;
        }

        public void ResetPets(IEnumerable<Pet> pets)
        {
            using var scope = Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AnimalDbContext>();
            context.Pets.RemoveRange(context.Pets);
            context.SaveChanges();
            context.Pets.AddRange(pets);
            context.SaveChanges();
        }

        public void ResetUserData(IEnumerable<User> users, IEnumerable<Notification>? notifications = null, IEnumerable<Appointment>? appointments = null)
        {
            using var scope = Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AnimalDbContext>();
            context.Notifications.RemoveRange(context.Notifications);
            context.Appointments.RemoveRange(context.Appointments);
            context.Users.RemoveRange(context.Users);
            context.SaveChanges();
            context.Users.AddRange(users);
            context.SaveChanges();
            if (notifications is not null) context.Notifications.AddRange(notifications);
            if (appointments is not null) context.Appointments.AddRange(appointments);
            context.SaveChanges();
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (disposing) _connection.Dispose();
        }
    }
}
