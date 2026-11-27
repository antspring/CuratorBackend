using System.ComponentModel.DataAnnotations;
using API.Profiles;
using Application.Mentees;
using Domain.Mentees;

namespace API.Mentees;

public sealed class MenteeProfilePatchRequest : IValidatableObject
{
    public MenteePersonalRequest? Personal { get; init; }

    [MaxLength(MenteeAnswers.MaxOwnedSkills)]
    public MenteeOwnedSkillRequest?[]? OwnedSkills { get; init; }

    [MaxLength(MenteeProfile.MaxGoals)]
    public MenteeGoalRequest?[]? Goals { get; init; }

    public ProfileAvailabilityRequest? Availability { get; init; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Personal is null && OwnedSkills is null && Goals is null && Availability is null)
        {
            yield return new ValidationResult("Укажите хотя бы одну плашку профиля.");
        }
    }

    public MenteeProfilePatch ToPatch() =>
        new(Personal?.ToInput(),
            OwnedSkills is null ? null : MenteeOwnedSkillRequest.ToChoices(OwnedSkills),
            Goals is null ? null : MenteeGoalRequest.ToChoices(Goals),
            Availability?.ToAvailability());
}
