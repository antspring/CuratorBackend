using System.ComponentModel.DataAnnotations;
using Application.Mentors;
using Domain.Common;
using Domain.Mentors;

namespace API.Mentors;

public sealed class MentorProfilePatchRequest : IValidatableObject
{
    [MaxLength(MentorProfile.MaxWorkplaces)]
    public MentorWorkplaceRequest?[]? Workplaces { get; init; }

    [MaxLength(MentorAnswers.MaxSkills)]
    public MentorSkillRequest?[]? Skills { get; init; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Workplaces is null && Skills is null)
        {
            yield return new ValidationResult("Укажите хотя бы одну плашку профиля.");
        }
    }

    public MentorProfilePatch ToPatch() => new(
        Workplaces?.Select((workplace, index) => workplace?.ToChoice()
            ?? throw new DomainException($"Workplaces[{index}]", "Заполните данные места работы.")).ToArray(),
        Skills is null ? null : MentorSkillRequest.ToChoices(Skills, nameof(Skills)));
}

public sealed class MentorWorkplaceRequest
{
    public Guid? Id { get; init; }

    [Required, MaxLength(MentorWorkplace.TextMaxLength)]
    public string Company { get; init; } = null!;

    [Required, MaxLength(MentorWorkplace.TextMaxLength)]
    public string Title { get; init; } = null!;

    [Required]
    public int? StartYear { get; init; }

    public int? EndYear { get; init; }

    [Required]
    public bool? IsCurrent { get; init; }

    public MentorWorkplaceInput ToChoice() =>
        new(Id, Company, Title, StartYear!.Value, EndYear, IsCurrent!.Value);
}
