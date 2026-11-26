namespace Domain.Mentees;

public readonly record struct MenteeOwnedSkillChoice(Guid SkillId, Guid SkillLevelId);

public sealed class MenteeOwnedSkill
{
    private MenteeOwnedSkill()
    {
    }

    internal MenteeOwnedSkill(Guid menteeId, MenteeOwnedSkillChoice choice)
    {
        MenteeId = menteeId;
        SkillId = choice.SkillId;
        SkillLevelId = choice.SkillLevelId;
    }

    public Guid MenteeId { get; private set; }

    public Guid SkillId { get; private set; }

    public Guid SkillLevelId { get; internal set; }
}
