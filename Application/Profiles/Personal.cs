using Domain.Users;

namespace Application.Profiles;

public sealed record PersonalInput(
    string FirstName,
    string LastName,
    int Age,
    Guid CityId,
    Guid LanguageId,
    Guid? PhotoFileId);

public sealed record PersonalView(
    string FirstName,
    string LastName,
    int Age,
    Guid CityId,
    Guid LanguageId,
    Guid? PhotoFileId,
    Uri? PhotoUrl)
{
    public static PersonalView From(UserProfile profile, Uri? photoUrl) =>
        new(profile.FirstName, profile.LastName, profile.Age, profile.CityId, profile.LanguageId, profile.PhotoFileId, photoUrl);
}
