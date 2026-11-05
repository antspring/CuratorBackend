using Domain.Common;

namespace Domain.Users;

public sealed class UserProfile
{
    public const int NameMaxLength = 100;
    public const int TimezoneMaxLength = 100;

    private UserProfile()
    {
    }

    public UserProfile(Guid userId, string firstName, string? lastName, string timezone, DateTimeOffset now)
    {
        if (userId == Guid.Empty)
        {
            throw new DomainException(nameof(UserId), "Идентификатор пользователя не задан.");
        }

        UserId = userId;
        CreatedAt = now.ToUniversalTime();
        Rename(firstName, lastName, now);
        ChangeTimezone(timezone, now);
    }

    public Guid UserId { get; private set; }

    public string FirstName { get; private set; } = null!;

    public string? LastName { get; private set; }

    public string Timezone { get; private set; } = null!;

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public void Rename(string firstName, string? lastName, DateTimeOffset now)
    {
        FirstName = RequireName(firstName, nameof(FirstName));
        LastName = string.IsNullOrWhiteSpace(lastName) ? null : LimitName(lastName.Trim(), nameof(LastName));
        UpdatedAt = now.ToUniversalTime();
    }

    public void ChangeTimezone(string timezone, DateTimeOffset now)
    {
        var trimmed = timezone?.Trim();

        if (string.IsNullOrEmpty(trimmed) || trimmed.Length > TimezoneMaxLength
            || !TimeZoneInfo.TryFindSystemTimeZoneById(trimmed, out _))
        {
            throw new DomainException(nameof(Timezone), "Укажите часовой пояс IANA, например Asia/Yekaterinburg.");
        }

        Timezone = trimmed;
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
