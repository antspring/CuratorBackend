using Domain.Common;
using Domain.Files;

namespace Domain.Users;

public sealed class UserProfile
{
    public const int NameMaxLength = 100;
    public const int MinAge = 14;
    public const int MaxAge = 100;

    private UserProfile()
    {
    }

    public UserProfile(Guid userId, PersonalData personal, DateTimeOffset now)
    {
        if (userId == Guid.Empty)
        {
            throw new DomainException(nameof(UserId), "Идентификатор пользователя не задан.");
        }

        UserId = userId;
        CreatedAt = now.ToUniversalTime();
        Update(personal, now);
    }

    public Guid UserId { get; private set; }

    public string FirstName { get; private set; } = null!;

    public string LastName { get; private set; } = null!;

    public short Age { get; private set; }

    public Guid CityId { get; private set; }

    public Guid LanguageId { get; private set; }

    public Guid? PhotoFileId { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public void Update(PersonalData personal, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(personal);

        if (personal.Age is < MinAge or > MaxAge)
        {
            throw new DomainException(nameof(Age), $"Возраст — от {MinAge} до {MaxAge} лет.");
        }

        if (personal.Photo is { } photo && (photo.OwnerId != UserId || photo.Purpose != FilePurposes.Avatar))
        {
            throw new DomainException(nameof(PhotoFileId), "Фото должно быть вашим загруженным аватаром.");
        }

        FirstName = RequireName(personal.FirstName, nameof(FirstName));
        LastName = RequireName(personal.LastName, nameof(LastName));
        Age = (short)personal.Age;
        CityId = RequireId(personal.CityId, nameof(CityId));
        LanguageId = RequireId(personal.LanguageId, nameof(LanguageId));
        PhotoFileId = personal.Photo?.Id;
        UpdatedAt = now.ToUniversalTime();
    }

    private static string RequireName(string? value, string field)
    {
        var trimmed = value?.Trim();

        if (string.IsNullOrEmpty(trimmed))
        {
            throw new DomainException(field, "Поле не может быть пустым.");
        }

        return trimmed.Length <= NameMaxLength
            ? trimmed
            : throw new DomainException(field, $"Не длиннее {NameMaxLength} символов.");
    }

    private static Guid RequireId(Guid value, string field) =>
        value != Guid.Empty ? value : throw new DomainException(field, "Значение не выбрано.");
}
