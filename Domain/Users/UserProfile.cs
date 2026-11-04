using Domain.Common;

namespace Domain.Users;

public sealed class UserProfile
{
    public const int NameMaxLength = 100;
    public const int TimeZoneMaxLength = 100;

    private UserProfile()
    {
    }

    public UserProfile(Guid userId, string firstName, string? lastName, string timeZone, DateTimeOffset now)
    {
        if (userId == Guid.Empty)
        {
            throw new DomainException(nameof(UserId), "Идентификатор пользователя не задан.");
        }

        UserId = userId;
        CreatedAt = now.ToUniversalTime();
        Rename(firstName, lastName, now);
        ChangeTimeZone(timeZone, now);
    }

    public Guid UserId { get; private set; }

    public string FirstName { get; private set; } = null!;

    public string? LastName { get; private set; }

    public string TimeZone { get; private set; } = null!;

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public void Rename(string firstName, string? lastName, DateTimeOffset now)
    {
        FirstName = RequireName(firstName, nameof(FirstName));
        LastName = string.IsNullOrWhiteSpace(lastName) ? null : LimitName(lastName.Trim(), nameof(LastName));
        UpdatedAt = now.ToUniversalTime();
    }

    public void ChangeTimeZone(string timeZone, DateTimeOffset now)
    {
        var trimmed = timeZone?.Trim();

        if (string.IsNullOrEmpty(trimmed) || trimmed.Length > TimeZoneMaxLength
            || !TimeZoneInfo.TryFindSystemTimeZoneById(trimmed, out _))
        {
            throw new DomainException(nameof(TimeZone), "Укажите часовой пояс IANA, например Asia/Yekaterinburg.");
        }

        TimeZone = trimmed;
        UpdatedAt = now.ToUniversalTime();
    }

    private static string RequireName(string? value, string field)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new DomainException(field, "Имя не может быть пустым.");
        }

        return LimitName(value.Trim(), field);
    }

    private static string LimitName(string value, string field) =>
        value.Length <= NameMaxLength
            ? value
            : throw new DomainException(field, $"Не длиннее {NameMaxLength} символов.");
}
