using Animal.Data.Repositories;
using Animal.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Animal.Data.Context
{
    public class AnimalDbContext : DbContext, IUnitOfWork
    {
        public AnimalDbContext(DbContextOptions<AnimalDbContext> options) : base(options)
        {
        }

        public DbSet<Shelter> Shelters => Set<Shelter>();
        public DbSet<Pet> Pets => Set<Pet>();
        public DbSet<User> Users => Set<User>();
        public DbSet<AdoptionApplication> AdoptionApplications => Set<AdoptionApplication>();
        public DbSet<Appointment> Appointments => Set<Appointment>();
        public DbSet<Notification> Notifications => Set<Notification>();

        // Each entity's mapping lives in its own IEntityTypeConfiguration class in Configurations/.
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(AnimalDbContext).Assembly);
        }
    }
}
