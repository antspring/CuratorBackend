using System.ComponentModel.DataAnnotations;
using API.Profiles;
using Application.Mentors;
using Domain.Mentors;

namespace API.Mentors;

public sealed class MentorProfileRequest
{
    [Required]
    public PersonalInfoRequest Personal { get; init; } = null!;

    [Required]
    public Guid? PositionId { get; init; }

    [Required]
    public Guid? ExperienceId { get; init; }

    [Required]
    [MinLength(1)]
    [MaxLength(MentorAnswers.MaxSkills)]
    public MentorSkillRequest?[] OwnedSkills { get; init; } = null!;

    [Required]
    [MinLength(1)]
    [MaxLength(MentorAnswers.MaxDirections)]
    public Guid[] DirectionIds { get; init; } = null!;

    [Required]
    public Guid? FormatId { get; init; }

    [Required]
    public Guid? AvailabilityId { get; init; }

    [Required]
    public Guid? PricingModelId { get; init; }

    [Required]
    [RegularExpression(Money.Pattern)]
    public string Price { get; init; } = null!;

    [Required]
    public bool? HasFreeTrial { get; init; }

    public SaveMentorProfileCommand ToCommand(Guid userId) =>
        new(
            userId,
            Personal.ToInput(),
            PositionId!.Value,
            ExperienceId!.Value,
            MentorSkillRequest.ToChoices(OwnedSkills, nameof(OwnedSkills)),
            DirectionIds,
            FormatId!.Value,
            AvailabilityId!.Value,
            PricingModelId!.Value,
            Money.Parse(Price)!.Value,
            HasFreeTrial!.Value);
}
