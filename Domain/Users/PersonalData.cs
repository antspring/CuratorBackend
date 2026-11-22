using Domain.Files;

namespace Domain.Users;

public sealed record PersonalData(
    string FirstName,
    string LastName,
    int Age,
    Guid CityId,
    Guid LanguageId,
    StoredFile? Photo);
