using Animal.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Animal.Data.Configurations
{
    public class ProfilePictureConfiguration : IEntityTypeConfiguration<ProfilePicture>
    {
        public void Configure(EntityTypeBuilder<ProfilePicture> builder)
        {
            builder.ToTable("ProfilePictures");
            builder.HasKey(p => p.UserId);

            builder.Property(p => p.ContentType).IsRequired().HasMaxLength(100);
            builder.Property(p => p.Bytes).IsRequired();
            builder.Property(p => p.UpdatedAt).IsRequired();

            builder.HasOne<User>()
                .WithOne()
                .HasForeignKey<ProfilePicture>(p => p.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
