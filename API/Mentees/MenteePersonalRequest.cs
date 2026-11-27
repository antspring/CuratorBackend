using System.ComponentModel.DataAnnotations;
using Application.Mentees;
using Domain.Mentees;
using Domain.Users;

namespace API.Mentees;

public sealed class MenteePersonalRequest
{
    [Required, MaxLength(PersonalData.NameMaxLength)]
    public string FirstName { get; init; } = null!;

    [Required, MaxLength(PersonalData.NameMaxLength)]
    public string LastName { get; init; } = null!;

    [Required, Range(PersonalData.MinAge, PersonalData.MaxAge)]
    public int? Age { get; init; }

    [Required]
    public Guid? CityId { get; init; }

    public Guid? PhotoFileId { get; init; }

    [Required, MaxLength(MenteeProfile.StatusMaxLength)]
    public string Status { get; init; } = null!;

    [Required, MaxLength(MenteeProfile.EducationMaxLength)]
    public string University { get; init; } = null!;

    [MaxLength(MenteeProfile.EducationMaxLength)]
    public string? Specialty { get; init; }

    [MaxLength(MenteeProfile.AboutMeMaxLength)]
    public string? AboutMe { get; init; }

    public MenteePersonalInput ToInput() =>
        new(FirstName, LastName, Age!.Value, CityId!.Value, PhotoFileId, Status, University, Specialty, AboutMe);
}
