using System.ComponentModel.DataAnnotations;
using API.Profiles;
using Application.Mentees;
using Domain.Mentees;

namespace API.Mentees;

public sealed class MenteeProfileRequest
{
    [Required]
    public PersonalInfoRequest Personal { get; init; } = null!;

    [Required]
    [MinLength(1)]
    [MaxLength(MenteeAnswers.MaxSkills)]
    public Guid[] SkillIds { get; init; } = null!;

    [Required, MinLength(1), MaxLength(MenteeAnswers.MaxOwnedSkills)]
    public MenteeOwnedSkillRequest?[] OwnedSkills { get; init; } = null!;

    [Required]
    [MinLength(1)]
    [MaxLength(MenteeAnswers.MaxTasks)]
    public Guid[] TaskIds { get; init; } = null!;

    [Required]
    public Guid? FormatId { get; init; }

    [Required]
    public Guid? AvailabilityId { get; init; }

    [RegularExpression(Money.Pattern)]
    public string? BudgetFrom { get; init; }

    [RegularExpression(Money.Pattern)]
    public string? BudgetTo { get; init; }

    [Required, MinLength(1), MaxLength(MenteeProfile.MaxGoals)]
    public MenteeGoalRequest?[] Goals { get; init; } = null!;

    public SaveMenteeProfileCommand ToCommand(Guid userId) =>
        new(
            userId,
            Personal.ToInput(),
            SkillIds,
            MenteeOwnedSkillRequest.ToChoices(OwnedSkills),
            TaskIds,
            FormatId!.Value,
            AvailabilityId!.Value,
            Money.Parse(BudgetFrom),
            Money.Parse(BudgetTo),
            MenteeGoalRequest.ToChoices(Goals));
}
