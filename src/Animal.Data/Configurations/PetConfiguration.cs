using Animal.Data.SeedData;
using Animal.Domain.Entities;
using Animal.Domain.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Animal.Data.Configurations
{
    public class PetConfiguration : IEntityTypeConfiguration<Pet>
    {
        public void Configure(EntityTypeBuilder<Pet> builder)
        {
            builder.ToTable("Pets");
            builder.HasKey(p => p.Id);

            // Column sizes come from the domain rules so the database and the validation always agree.
            builder.Property(p => p.Name).IsRequired().HasMaxLength(PetRules.MaxNameLength);
            builder.Property(p => p.Species).IsRequired().HasMaxLength(PetRules.MaxSpeciesLength);
            builder.Property(p => p.Breed).IsRequired().HasMaxLength(PetRules.MaxBreedLength);

            // Restrict instead of cascade: deleting a shelter that still has pets should fail, not wipe the pets.
            builder.HasOne<Shelter>()
                .WithMany()
                .HasForeignKey(p => p.ShelterId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(p => p.Species);

            builder.HasData(PetSeedData.GetPets());
        }
    }
}
