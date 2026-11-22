using Domain.Catalogs;
using Domain.Files;
using Domain.Users;
using Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

internal sealed class UserProfileConfiguration : IEntityTypeConfiguration<UserProfile>
{
    public void Configure(EntityTypeBuilder<UserProfile> builder)
    {
        builder.ToTable("user_profiles");
        builder.HasKey(profile => profile.UserId);

        builder.Property(profile => profile.UserId).ValueGeneratedNever();
        builder.Property(profile => profile.FirstName).HasMaxLength(PersonalData.NameMaxLength);
        builder.Property(profile => profile.LastName).HasMaxLength(PersonalData.NameMaxLength);

        builder.HasOne<ApplicationUser>()
            .WithOne()
            .HasForeignKey<UserProfile>(profile => profile.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<City>()
            .WithMany()
            .HasForeignKey(profile => profile.CityId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<CatalogItem>()
            .WithMany()
            .HasForeignKey(profile => profile.LanguageId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<StoredFile>()
            .WithMany()
            .HasForeignKey(profile => profile.PhotoFileId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
