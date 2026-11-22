using System.ComponentModel.DataAnnotations;
using Application.Profiles;
using Domain.Users;

namespace API.Profiles;

public sealed class PersonalInfoRequest
{
    [Required]
    [MaxLength(PersonalData.NameMaxLength)]
    public string FirstName { get; init; } = null!;

    [Required]
    [MaxLength(PersonalData.NameMaxLength)]
    public string LastName { get; init; } = null!;

    [Required]
    [Range(PersonalData.MinAge, PersonalData.MaxAge)]
    public int? Age { get; init; }

    [Required]
    public Guid? CityId { get; init; }

    [Required]
    public Guid? LanguageId { get; init; }

    public Guid? PhotoFileId { get; init; }

    public PersonalInput ToInput() =>
        new(FirstName, LastName, Age!.Value, CityId!.Value, LanguageId!.Value, PhotoFileId);
}
