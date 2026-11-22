using System.ComponentModel.DataAnnotations;
using Domain.Mentors;

namespace API.Mentors;

public sealed class MentorSkillRequest
{
    [Required]
    public Guid? SkillId { get; init; }

    [Required]
    public Guid? SkillLevelId { get; init; }

    public MentorSkillChoice ToChoice() => new(SkillId!.Value, SkillLevelId!.Value);
}
