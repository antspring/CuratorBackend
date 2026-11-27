namespace Domain.Mentors;

public sealed class MentorExpectedSkill
{
    private MentorExpectedSkill()
    {
    }

    internal MentorExpectedSkill(Guid mentorId, Guid skillId, Guid skillLevelId)
    {
        MentorId = mentorId;
        SkillId = skillId;
        SkillLevelId = skillLevelId;
    }

    public Guid MentorId { get; private set; }

    public Guid SkillId { get; private set; }

    public Guid SkillLevelId { get; internal set; }
}
