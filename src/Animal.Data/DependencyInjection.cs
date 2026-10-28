using Animal.Data.Context;
using Animal.Data.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Animal.Data
{
    public static class DependencyInjection
    {
        // Registers the DbContext, the unit of work and the repositories. Called once from the API's Program.cs.
        public static IServiceCollection AddAnimalData(this IServiceCollection services, string connectionString)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

            services.AddDbContext<AnimalDbContext>(options => options.UseSqlServer(connectionString));
            services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<AnimalDbContext>());
            services.AddScoped<IPetRepository, PetRepository>();

            return services;
        }
    }
}
