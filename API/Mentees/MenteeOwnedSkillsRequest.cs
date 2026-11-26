using System.ComponentModel.DataAnnotations;
using Domain.Common;
using Domain.Mentees;

namespace API.Mentees;

public sealed class MenteeOwnedSkillsRequest
{
    [Required, MaxLength(MenteeProfile.MaxOwnedSkills)]
    public MenteeOwnedSkillRequest?[] OwnedSkills { get; init; } = null!;

    public MenteeOwnedSkillChoice[] ToChoices() => OwnedSkills
        .Select(skill => skill?.ToChoice() ?? throw new DomainException(nameof(OwnedSkills), "Укажите навык и его уровень."))
        .ToArray();
}

public sealed class MenteeOwnedSkillRequest
{
    [Required]
    public Guid? SkillId { get; init; }

    [Required]
    public Guid? SkillLevelId { get; init; }

    public MenteeOwnedSkillChoice ToChoice() => new(SkillId!.Value, SkillLevelId!.Value);
}
