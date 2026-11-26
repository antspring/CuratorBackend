using Domain.Common;

namespace Domain.Users;

public sealed class UserProfile
{
    private UserProfile()
    {
    }

    public UserProfile(Guid userId, PersonalData personal, DateTimeOffset now)
    {
        UserId = Require.Id(userId, nameof(UserId));
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

        if (personal.Photo is { } photo && photo.OwnerId != UserId)
        {
            throw new DomainException(nameof(PhotoFileId), "Фото должно быть вашим загруженным аватаром.");
        }

        FirstName = personal.FirstName;
        LastName = personal.LastName;
        Age = personal.Age;
        CityId = personal.CityId;
        LanguageId = personal.LanguageId;
        PhotoFileId = personal.PhotoFileId;
        MarkUpdated(now);
    }

    public void MarkUpdated(DateTimeOffset now) => UpdatedAt = now.ToUniversalTime();
}
