using Animal.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Animal.Data.Configurations
{
    public class AppointmentConfiguration : IEntityTypeConfiguration<Appointment>
    {
        public void Configure(EntityTypeBuilder<Appointment> builder)
        {
            builder.ToTable("Appointments");
            builder.HasKey(a => a.Id);

            builder.Property(a => a.Status).HasConversion<string>().HasMaxLength(20);

            // All three are Restrict: SQL Server rejects tables with more than one cascade path to the same row.
            builder.HasOne<User>()
                .WithMany()
                .HasForeignKey(a => a.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne<Pet>()
                .WithMany()
                .HasForeignKey(a => a.PetId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne<Shelter>()
                .WithMany()
                .HasForeignKey(a => a.ShelterId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
