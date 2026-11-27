using Domain.Catalogs;
using Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

internal static class ProfileAvailabilityConfiguration
{
    public static void Configure<TProfile>(OwnedNavigationBuilder<TProfile, ProfileAvailability> availability,
        string tableName, string ownerId) where TProfile : class
    {
        availability.ToTable(tableName);
        availability.WithOwner().HasForeignKey(ownerId);
        availability.Property(schedule => schedule.TimeZoneId).HasMaxLength(ProfileAvailability.TimeZoneIdMaxLength);
        availability.HasOne<City>().WithMany().HasForeignKey(schedule => schedule.MeetingCityId).OnDelete(DeleteBehavior.Restrict);
    }
}
