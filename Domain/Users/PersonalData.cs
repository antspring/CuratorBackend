using Domain.Common;
using Domain.Files;

namespace Domain.Users;

public sealed class PersonalData
{
    public const int NameMaxLength = 100;
    public const int MinAge = 14;
    public const int MaxAge = 100;

    public PersonalData(string firstName, string lastName, int age, Guid cityId, Guid languageId, StoredFile? photo)
    {
        if (age is < MinAge or > MaxAge)
        {
            throw new DomainException(nameof(Age), $"Возраст — от {MinAge} до {MaxAge} лет.");
        }

        if (photo is not null && photo.Purpose != FilePurposes.Avatar)
        {
            throw new DomainException(nameof(PhotoFileId), "Фото должно быть загружено как аватар.");
        }

        FirstName = Require.Text(firstName, 1, NameMaxLength, nameof(FirstName));
        LastName = Require.Text(lastName, 1, NameMaxLength, nameof(LastName));
        Age = (short)age;
        CityId = Require.Id(cityId, nameof(CityId));
        LanguageId = Require.Id(languageId, nameof(LanguageId));
        Photo = photo;
    }

    public string FirstName { get; }

    public string LastName { get; }

    public short Age { get; }

    public Guid CityId { get; }

    public Guid LanguageId { get; }

    public StoredFile? Photo { get; }

    public Guid? PhotoFileId => Photo?.Id;
}
