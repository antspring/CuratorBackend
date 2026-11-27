using System.ComponentModel.DataAnnotations;
using System.Globalization;
using Domain.Common;
using Domain.Users;

namespace API.Profiles;

public sealed class ProfileAvailabilityRequest
{
    private const string TimeFormat = "HH:mm";
    private const string TimePattern = "^(?:[01][0-9]|2[0-3]):[0-5][0-9]$";

    [Required, MinLength(1), MaxLength(7)]
    public DayOfWeek[] Days { get; init; } = null!;

    [Required, RegularExpression(TimePattern)]
    public string StartTime { get; init; } = null!;

    [Required, RegularExpression(TimePattern)]
    public string EndTime { get; init; } = null!;

    [Required, MaxLength(ProfileAvailability.TimeZoneIdMaxLength)]
    public string TimeZoneId { get; init; } = null!;

    [Required, MinLength(1), MaxLength(2)]
    public MeetingFormat[] Formats { get; init; } = null!;

    public Guid? MeetingCityId { get; init; }

    public ProfileAvailability ToAvailability()
    {
        try
        {
            return new(Days, TimeOnly.ParseExact(StartTime, TimeFormat, CultureInfo.InvariantCulture),
                TimeOnly.ParseExact(EndTime, TimeFormat, CultureInfo.InvariantCulture), TimeZoneId, Formats, MeetingCityId);
        }
        catch (DomainException exception)
        {
            throw new DomainException($"Availability.{exception.Field}", exception.Message);
        }
    }
}
