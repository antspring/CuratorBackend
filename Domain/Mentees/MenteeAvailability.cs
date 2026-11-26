using Domain.Common;

namespace Domain.Mentees;

public enum MeetingFormat
{
    Online,
    Offline,
}

public sealed class MenteeAvailability
{
    public const int TimeZoneIdMaxLength = 100;

    private MenteeAvailability()
    {
    }

    public MenteeAvailability(
        IReadOnlyList<DayOfWeek> days,
        TimeOnly startTime,
        TimeOnly endTime,
        string timeZoneId,
        IReadOnlyList<MeetingFormat> formats,
        Guid? meetingCityId)
    {
        Days = RequireSelections(days, nameof(Days)).OrderBy(day => ((int)day + 6) % 7).ToArray();

        if (endTime <= startTime)
        {
            throw new DomainException(nameof(EndTime), "Время окончания должно быть позже начала в тот же день.");
        }

        StartTime = startTime;
        EndTime = endTime;
        TimeZoneId = Require.Text(timeZoneId, 1, TimeZoneIdMaxLength, nameof(TimeZoneId));
        if (!TimeZoneInfo.TryFindSystemTimeZoneById(TimeZoneId, out var zone) || (!zone.HasIanaId && TimeZoneId != "UTC"))
        {
            throw new DomainException(nameof(TimeZoneId), "Укажите действительный часовой пояс IANA.");
        }

        Formats = RequireSelections(formats, nameof(Formats)).Order().ToArray();
        if (Formats.Contains(MeetingFormat.Offline))
        {
            MeetingCityId = Require.Id(meetingCityId.GetValueOrDefault(), nameof(MeetingCityId));
        }
        else if (meetingCityId is not null)
        {
            throw new DomainException(nameof(MeetingCityId), "Город встречи указывается для офлайн-формата.");
        }
    }

    public IReadOnlyList<DayOfWeek> Days { get; private set; } = [];

    public TimeOnly StartTime { get; private set; }

    public TimeOnly EndTime { get; private set; }

    public string TimeZoneId { get; private set; } = null!;

    public IReadOnlyList<MeetingFormat> Formats { get; private set; } = [];

    public Guid? MeetingCityId { get; private set; }

    private static IReadOnlyList<T> RequireSelections<T>(IReadOnlyList<T> values, string field) where T : struct, Enum
    {
        if (values.Count == 0 || values.Any(value => !Enum.IsDefined(value)) || values.Distinct().Count() != values.Count)
        {
            throw new DomainException(field, "Выберите хотя бы одно допустимое значение без повторений.");
        }

        return values;
    }
}
