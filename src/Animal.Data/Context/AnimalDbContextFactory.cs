using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Animal.Data.Context
{
    public class AnimalDbContextFactory : IDesignTimeDbContextFactory<AnimalDbContext>
    {
        public AnimalDbContext CreateDbContext(string[] args)
        {
            var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__AnimalDb")
                ?? "Server=(localdb)\\mssqllocaldb;Database=AnimalAdoption;Trusted_Connection=True;TrustServerCertificate=True";

            var optionsBuilder = new DbContextOptionsBuilder<AnimalDbContext>();
            optionsBuilder.UseSqlServer(connectionString);

            return new AnimalDbContext(optionsBuilder.Options);
        }
    }
}