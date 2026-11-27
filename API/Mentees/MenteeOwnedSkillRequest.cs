using System.ComponentModel.DataAnnotations;
using Domain.Common;
using Domain.Mentees;

namespace API.Mentees;

public sealed class MenteeOwnedSkillRequest
{
    [Required]
    public Guid? SkillId { get; init; }

    [Required]
    public Guid? SkillLevelId { get; init; }

    public static MenteeOwnedSkillChoice[] ToChoices(IEnumerable<MenteeOwnedSkillRequest?> skills) => skills
        .Select((skill, index) => skill?.ToChoice()
            ?? throw new DomainException($"OwnedSkills[{index}]", "Укажите навык и его уровень."))
        .ToArray();

    private MenteeOwnedSkillChoice ToChoice() => new(SkillId!.Value, SkillLevelId!.Value);
}
