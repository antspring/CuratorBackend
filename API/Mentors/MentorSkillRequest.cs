using System.ComponentModel.DataAnnotations;
using Domain.Common;
using Domain.Mentors;

namespace API.Mentors;

public sealed class MentorSkillRequest
{
    [Required]
    public Guid? SkillId { get; init; }

    [Required]
    public Guid? SkillLevelId { get; init; }

    public static MentorSkillChoice[] ToChoices(IEnumerable<MentorSkillRequest?> requests, string field) => requests
        .Select((skill, index) => skill?.ToChoice()
            ?? throw new DomainException($"{field}[{index}]", "Заполните данные навыка."))
        .ToArray();

    private MentorSkillChoice ToChoice() => new(SkillId!.Value, SkillLevelId!.Value);
}
