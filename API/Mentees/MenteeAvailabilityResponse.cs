using System.Globalization;
using API.Common;
using Domain.Mentees;

namespace API.Mentees;

public sealed record MenteeAvailabilityResponse(
    IReadOnlyList<DayOfWeek> Days,
    string StartTime,
    string EndTime,
    string TimeZoneId,
    IReadOnlyList<MeetingFormat> Formats,
    LookupResponse? MeetingCity)
{
    public static MenteeAvailabilityResponse? From(MenteeAvailability? availability, LookupResponse? meetingCity) =>
        availability is null ? null : new(
            availability.Days,
            availability.StartTime.ToString("HH:mm", CultureInfo.InvariantCulture),
            availability.EndTime.ToString("HH:mm", CultureInfo.InvariantCulture),
            availability.TimeZoneId,
            availability.Formats,
            meetingCity);
}
