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
        builder.Property(profile => profile.FirstName).HasMaxLength(UserProfile.NameMaxLength);
        builder.Property(profile => profile.LastName).HasMaxLength(UserProfile.NameMaxLength);
        builder.Property(profile => profile.TimeZone)
            .HasColumnName("timezone")
            .HasMaxLength(UserProfile.TimeZoneMaxLength);

        builder.HasOne<ApplicationUser>()
            .WithOne()
            .HasForeignKey<UserProfile>(profile => profile.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
