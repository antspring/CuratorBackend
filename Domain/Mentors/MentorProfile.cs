using Domain.Common;

namespace Domain.Mentors;

public sealed class MentorProfile
{
    private readonly List<MentorSkill> skills = [];
    private readonly List<MentorDirection> directions = [];

    private MentorProfile()
    {
    }

    public MentorProfile(Guid userId, MentorAnswers answers)
    {
        UserId = Require.Id(userId, nameof(UserId));
        Update(answers);
    }

    public Guid UserId { get; private set; }

    public Guid PositionId { get; private set; }

    public Guid ExperienceId { get; private set; }

    public Guid FormatId { get; private set; }

    public Guid AvailabilityId { get; private set; }

    public Guid PricingModelId { get; private set; }

    public decimal Price { get; private set; }

    public bool HasFreeTrial { get; private set; }

    public IReadOnlyCollection<MentorSkill> Skills => skills;

    public IReadOnlyCollection<MentorDirection> Directions => directions;

    public void Update(MentorAnswers answers)
    {
        ArgumentNullException.ThrowIfNull(answers);

        PositionId = answers.PositionId;
        ExperienceId = answers.ExperienceId;
        FormatId = answers.FormatId;
        AvailabilityId = answers.AvailabilityId;
        PricingModelId = answers.PricingModelId;
        Price = answers.Price;
        HasFreeTrial = answers.HasFreeTrial;

        SyncSkills(answers.Skills);
        SyncDirections(answers.DirectionIds);
    }

    private void SyncSkills(IReadOnlyList<MentorSkillChoice> choices)
    {
        var levels = choices.ToDictionary(choice => choice.SkillId, choice => choice.SkillLevelId);

        skills.RemoveAll(skill => !levels.ContainsKey(skill.SkillId));

        foreach (var skill in skills)
        {
            skill.SkillLevelId = levels[skill.SkillId];
        }

        var existing = skills.Select(skill => skill.SkillId).ToHashSet();
        skills.AddRange(choices
            .Where(choice => !existing.Contains(choice.SkillId))
            .Select(choice => new MentorSkill(UserId, choice.SkillId, choice.SkillLevelId)));
    }

    private void SyncDirections(IReadOnlyList<Guid> directionIds)
    {
        directions.RemoveAll(direction => !directionIds.Contains(direction.DirectionId));

        var existing = directions.Select(direction => direction.DirectionId).ToHashSet();
        directions.AddRange(directionIds
            .Where(id => !existing.Contains(id))
            .Select(id => new MentorDirection(UserId, id)));
    }
}
