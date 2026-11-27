using System.Globalization;
using API.Common;
using Application.Catalogs;
using Domain.Users;

namespace API.Profiles;

public sealed record ProfileAvailabilityResponse(
    IReadOnlyList<DayOfWeek> Days,
    string StartTime,
    string EndTime,
    string TimeZoneId,
    IReadOnlyList<MeetingFormat> Formats,
    LookupResponse? MeetingCity)
{
    public static ProfileAvailabilityResponse? From(ProfileAvailability? availability, CatalogRef? meetingCity) =>
        availability is null ? null : new(
            availability.Days,
            availability.StartTime.ToString("HH:mm", CultureInfo.InvariantCulture),
            availability.EndTime.ToString("HH:mm", CultureInfo.InvariantCulture),
            availability.TimeZoneId,
            availability.Formats,
            meetingCity is null ? null : LookupResponse.From(meetingCity));
}
