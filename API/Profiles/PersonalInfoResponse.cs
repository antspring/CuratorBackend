using Application.Profiles;

namespace API.Profiles;

public sealed record PersonalInfoResponse(
    string FirstName,
    string LastName,
    int Age,
    Guid CityId,
    Guid LanguageId,
    Guid? PhotoFileId,
    Uri? PhotoUrl)
{
    public static PersonalInfoResponse From(PersonalView personal) =>
        new(personal.FirstName, personal.LastName, personal.Age, personal.CityId, personal.LanguageId, personal.PhotoFileId, personal.PhotoUrl);
}
