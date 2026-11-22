namespace Domain.Mentees;

public sealed class MenteeSkill
{
    private MenteeSkill()
    {
    }

    internal MenteeSkill(Guid menteeId, Guid skillId)
    {
        MenteeId = menteeId;
        SkillId = skillId;
    }

    public Guid MenteeId { get; private set; }

    public Guid SkillId { get; private set; }
}
