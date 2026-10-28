using Animal.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Animal.Data.Configurations
{
    public class AdoptionApplicationConfiguration : IEntityTypeConfiguration<AdoptionApplication>
    {
        public void Configure(EntityTypeBuilder<AdoptionApplication> builder)
        {
            builder.ToTable("AdoptionApplications");
            builder.HasKey(a => a.Id);

            // Stored as text ("Pending") so the table is readable and adding an enum value doesn't shift numbers.
            builder.Property(a => a.Status).HasConversion<string>().HasMaxLength(20);

            builder.HasOne<User>()
                .WithMany()
                .HasForeignKey(a => a.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne<Pet>()
                .WithMany()
                .HasForeignKey(a => a.PetId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
