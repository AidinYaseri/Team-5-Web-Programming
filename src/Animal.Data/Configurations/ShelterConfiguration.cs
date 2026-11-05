using Animal.Data.SeedData;
using Animal.Domain.Entities;
using Animal.Domain.Services;
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

            builder.Property(s => s.Name).IsRequired().HasMaxLength(ShelterRules.MaxNameLength);
            builder.Property(s => s.Address).IsRequired().HasMaxLength(ShelterRules.MaxAddressLength);
            builder.Property(s => s.PhoneNumber).IsRequired().HasMaxLength(ShelterRules.MaxPhoneLength);
            builder.Property(s => s.Email).IsRequired().HasMaxLength(ShelterRules.MaxEmailLength);

            builder.HasData(ShelterSeedData.GetShelters());
        }
    }
}
