using Animal.Data.SeedData;
using Animal.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Animal.Data.Configurations
{
    public class ShelterConfiguration : IEntityTypeConfiguration<Shelter>
    {
        public void Configure(EntityTypeBuilder<Shelter> builder)
        {
            builder.ToTable("Shelters");
            builder.HasKey(s => s.Id);

            builder.Property(s => s.Name).IsRequired().HasMaxLength(100);
            builder.Property(s => s.Address).IsRequired().HasMaxLength(200);
            builder.Property(s => s.PhoneNumber).IsRequired().HasMaxLength(20);
            builder.Property(s => s.Email).IsRequired().HasMaxLength(254);

            builder.HasData(ShelterSeedData.GetShelters());
        }
    }
}
